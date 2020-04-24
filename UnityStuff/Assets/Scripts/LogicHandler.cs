using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FoPra.model;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngineInternal;

[Serializable]
public class LogicHandler {
   private ComputeShader cs;
   private Model model;

   private const float margin = 1.05f;
   private Vector2[] data;
   private Vector2[] g1_dists_inner_precomputed;
   private Vector2[] g1_dists_outer_precomputed;
   private int angleSteps = 1;
   private int size;
   private Vector2[,] g2_dists_inner;
   private Vector2[,] g2_dists_outer;
   private Vector3[] absorbtions;
   private Vector3[] absorptionFactors;

   private int g1_handle;
   private int g2_handle;
   private int absorptions_handle;

   
   private ComputeBuffer inputBuffer;
   private ComputeBuffer outputBufferOuter;
   private ComputeBuffer outputBufferInner;
   private ComputeBuffer absorptionsBuffer;
   
   public LogicHandler(Model model, ComputeShader cs) {
      this.model = model;
      this.cs = cs;
      size = model.get_accuracy_resolution_size();
   }

   /**
    * Data generation,
    * - defines empty Vector2[] g1_dists for pre-calculation purposes;
    * - defines empty Vector3[] absorptions for absorptions per ???;
    * - sets handles
    */
   void initDataFields(int size) {
      var (a, b) = (-model.get_r_sample()*margin, model.get_r_sample()*margin);
      data = Enumerable.Range(0, size)
         .Select(i => Enumerable.Range(0, size)
            .Select(j => 
                  new Vector2(a + 1f*i*(b-a)/(size-1), a + 1f*j*(b-a)/(size-1))
               //new Vector2(i,j)
            ).ToArray())
         .SelectMany(arr => arr)
         .ToArray();
      
      if (model.settings.mode == Model.Modes.Point) {
         angleSteps = model.get_angles2D().Length;
      } else if (model.settings.mode == Model.Modes.Area) {
         angleSteps = (int) (model.detector.resolution.x * model.detector.resolution.y);
      } else if (model.settings.mode == Model.Modes.Integrated) {
         // TODO:
      }
      
      g1_dists_inner_precomputed = new Vector2[size*size];
      Array.Clear(g1_dists_inner_precomputed,0,size*size);
      g1_dists_outer_precomputed = new Vector2[size*size];
      Array.Clear(g1_dists_outer_precomputed,0,size*size);
      g2_dists_inner = new Vector2[angleSteps, size*size];
      Array.Clear(g2_dists_inner,0,size*size);
      g2_dists_outer = new Vector2[angleSteps, size*size];
      Array.Clear(g2_dists_outer,0,size*size);
      
      absorbtions = new Vector3[size*size];
      Array.Clear(absorbtions,0,size*size);
      absorptionFactors = new Vector3[angleSteps];
      
      cs.SetFloats("mu", model.get_mu_cell(), model.get_mu_sample());
      cs.SetFloat("r_cell", model.get_r_cell());
      cs.SetFloat("r_sample", model.get_r_sample());
      cs.SetFloat("r_cell_sq", model.get_r_cell_sq());
      cs.SetFloat("r_sample_sq", model.get_r_sample_sq());
      
      g1_handle = cs.FindKernel("g1_dists");
      g2_handle = cs.FindKernel("g2_dists");
      absorptions_handle = cs.FindKernel("Absorptions");

      
         
      

   }

   public void run_shader() {
      
      initDataFields(size);
      Debug.Log(data.Length.ToString());
      
      inputBuffer = new ComputeBuffer(data.Length, 8);
      outputBufferOuter = new ComputeBuffer(data.Length, 8);
      outputBufferInner = new ComputeBuffer(data.Length, 8);
      absorptionsBuffer = new ComputeBuffer(data.Length, 12);
      
      
      Debug.Log(data[0].ToString() + ", " + data[1].ToString() + ", " + data[2].ToString());

      inputBuffer.SetData(data);

      calculate_g1_dists(size);

      if (model.settings.mode == Model.Modes.Point) {
         for (int i = 0; i < angleSteps; i++) {
            outputBufferInner.SetData(g1_dists_inner_precomputed);
            outputBufferOuter.SetData(g1_dists_outer_precomputed);
            calculate_absorptions_2D(size, i);
            extractAbsorptionFactor(i);
            
         }
      } else if (model.settings.mode == Model.Modes.Area) {
         for (int i = 0; i < angleSteps; i++) {
            calculate_g2_dists(size, i);
         }
         for (int i = 0; i < angleSteps; i++) {
            calculate_g2_3D_dists(size, i);
         }
      } else if (model.settings.mode == Model.Modes.Integrated) {
         // TODO: 
      }

      for (int i = 0; i <  angleSteps; i++) {
         calculate_absorptions_2D(size, i);
      }
      
      writeAbsorbtionFactors(size);
      //writeData_Dists(size);
      inputBuffer.Release();
      outputBufferOuter.Release();
      outputBufferInner.Release();
      absorptionsBuffer.Release();
      
   }
   
   /**
    * g1 pass: Compute g1 dists in shader buffers
    */
   public void calculate_g1_dists(int size) {
      
      cs.SetBuffer(g1_handle, "segment", inputBuffer);
      cs.SetBuffer(g1_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(g1_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(g1_handle, size, 1, 1);
      outputBufferInner.GetData(g1_dists_inner_precomputed);
      outputBufferOuter.GetData(g1_dists_outer_precomputed);
   }

   /**
    * g2 pass: Add g2 dists to g1 dists in shader buffers
    */
   public void calculate_g2_dists(int size, int i) {
      
      cs.SetBuffer(g2_handle, "segment", inputBuffer);
      cs.SetFloat("cos", (float) Math.Cos((180 - 2 * model.get_angles2D()[i]) * Math.PI / 180));
      cs.SetFloat("sin", (float) Math.Sin((180 - 2 * model.get_angles2D()[i]) * Math.PI / 180));      
      cs.SetBuffer(g2_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(g2_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(g2_handle, size, 1, 1);
      Vector2[] tempInner = new Vector2[size*size];
      Vector2[] tempOuter = new Vector2[size*size];
      outputBufferInner.GetData(tempInner); // .add??
      outputBufferOuter.GetData(tempOuter); // .add??
      for (int j = 0; j < tempInner.Length; j++) {
         g2_dists_inner[i,j] = tempInner[j];
         g2_dists_outer[i,j] = tempOuter[j];
      }
   }

   public void calculate_g2_3D_dists(int size, int i) {
      
   }

   /**
    * absorptions pass: calculate absorptions from distances in shader buffers
    * (used case chosen in run-method)
    */
   public void calculate_absorptions_2D(int size, int i) {
      
      cs.SetBuffer(g2_handle, "segment", inputBuffer);
      cs.SetFloat("cos", (float) Math.Cos((180 - 2 * model.get_angles2D()[i]) * Math.PI / 180));
      cs.SetFloat("sin", (float) Math.Sin((180 - 2 * model.get_angles2D()[i]) * Math.PI / 180));      
      cs.SetBuffer(g2_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(g2_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(g2_handle, size, 1, 1);
      
      cs.SetBuffer(absorptions_handle, "absorptions", absorptionsBuffer);
      cs.SetBuffer(absorptions_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(absorptions_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(absorptions_handle, size, 1, 1);
      absorptionsBuffer.GetData(absorbtions);
   }

   void extractAbsorptionFactor(int j) {
      Vector3 res = new Vector3();
      Vector3 counterVec = new Vector3();
      for (int i = 0; i < size*size; i++) {
         if (absorbtions[i].x < 1f) {
            counterVec.x++;
            res.x = res.x + absorbtions[i].x;
         }
         if (absorbtions[i].y < 1f) {
            counterVec.y++;
            res.y = res.y + absorbtions[i].y;
         }
         if ((absorbtions[i].z < 1f)) {
            counterVec.z++;
            res.z = res.z + absorbtions[i].z;
         }
      }

      res.x = res.x / counterVec.x;
      res.y = res.y / counterVec.y;
      res.z = res.z / counterVec.z;
      absorptionFactors[j] = res;
   }

   void writeData_Dists(int size) {
      var res_str = "{";
      var sep = Path.DirectorySeparatorChar;
      for (int i = 0; i < angleSteps; i++) {
         res_str += "\"" + model.get_angles2D()[i] + "\":[";
         for (int j = 1; j < size*size; j++) {
            res_str += g2_dists_inner[i, j].ToString("G");
            if (j<size*size-1) {
               res_str += ",";
            }
         }
         res_str += "]";
         if (i < angleSteps-1) {
            res_str += ",";
         }
      }

      res_str = res_str.Replace("(", "[").Replace(")", "]") + "}";
      Debug.Log(res_str);
      File.WriteAllText($"Logs{sep}Distances2D{sep}Output n={size}.txt", res_str);

   }

   private void writeAbsorbtionFactors(int size) {
      var res_string = "Winkel\t A_{s,sc}\t A_{c,sc}\t A_{c,c}\n";
      var sep = Path.DirectorySeparatorChar;

      for (int i = 0; i < angleSteps; i++) {
         res_string += model.get_angles2D()[i].ToString("G") + "\t"
                                                             + absorptionFactors[i].x.ToString("G") + "\t"
                                                             + absorptionFactors[i].y.ToString("G") + "\t"
                                                             + absorptionFactors[i].z.ToString("G") + "\n";
      }
      File.WriteAllText($"Logs{sep}Distances2D{sep}Output n={size}.txt", res_string);
         
   }

}




































