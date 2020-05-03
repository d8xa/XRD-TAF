using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FoPra.model;
using FoPra.util;
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
   private int segmentResolution;
   private Vector2[,] g2_dists_inner;
   private Vector2[,] g2_dists_outer;
   private Vector3[] absorptions;
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
      segmentResolution = model.get_accuracy_resolution_size();
   }

   /**
    * Data generation,
    * - defines empty Vector2[] g1_dists for pre-calculation purposes;
    * - defines empty Vector3[] absorptions for absorptions per ???;
    * - sets handles
    */
   void initDataFields(int segmentRes) {
      data = MathTools.LinSpace2D(-model.get_r_cell()*margin, model.get_r_cell()*margin, segmentRes);
      Debug.Log(data.Length);
      
      // TODO: maybe use length of angle list directly instead.
      if (model.settings.mode == Model.Mode.Point)
         angleSteps = model.get_angles2D().Length;
      else if (model.settings.mode == Model.Mode.Area)
         angleSteps = (int) (model.detector.resolution.x * model.detector.resolution.y);
      else if (model.settings.mode == Model.Mode.Integrated)
      {
         // TODO:
      }
      else
         angleSteps = model.get_angles2D().Length;
      
      var segmentCount = segmentRes * segmentRes;
      g1_dists_inner_precomputed = new Vector2[segmentCount];
      Array.Clear(g1_dists_inner_precomputed,0,segmentCount);
      g1_dists_outer_precomputed = new Vector2[segmentCount];
      Array.Clear(g1_dists_outer_precomputed,0,segmentCount);
      g2_dists_inner = new Vector2[angleSteps, segmentCount];   // TODO: memory management. prio=mid.
      Array.Clear(g2_dists_inner,0, segmentCount);
      g2_dists_outer = new Vector2[angleSteps, segmentCount];
      Array.Clear(g2_dists_outer,0, segmentCount);
      
      absorptions = new Vector3[segmentCount];
      Array.Clear(absorptions,0, segmentCount);
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

   public void run_shader() 
   {
      initDataFields(segmentResolution);
      Debug.Log($"Running shader on resolution={segmentResolution}, mode={model.settings.mode}");
      
      inputBuffer = new ComputeBuffer(data.Length, 8);
      outputBufferOuter = new ComputeBuffer(data.Length, 8);
      outputBufferInner = new ComputeBuffer(data.Length, 8);
      absorptionsBuffer = new ComputeBuffer(data.Length, 12);
      inputBuffer.SetData(data);

      calculate_g1_dists();

      if (model.settings.mode == Model.Mode.Point) {
         for (int i = 0; i < angleSteps; i++) {
            outputBufferInner.SetData(g1_dists_inner_precomputed);
            outputBufferOuter.SetData(g1_dists_outer_precomputed);
            calculate_absorptions_2D(i);
            extractAbsorptionFactor(i, (float) 1E-14);
         }
         WriteAbsorptionFactors();
      } 
      else if (model.settings.mode == Model.Mode.Area) 
      {
         for (int i = 0; i < angleSteps; i++)
            calculate_g2_dists(i, true);
         for (int i = 0; i < angleSteps; i++)
            calculate_g2_3D_dists(segmentResolution, i);
      } 
      else if (model.settings.mode == Model.Mode.Integrated) 
      {
         // TODO.
      }
      else
      {
         for (int i = 0; i < angleSteps; i++) {
            outputBufferInner.SetData(g1_dists_inner_precomputed);
            outputBufferOuter.SetData(g1_dists_outer_precomputed);
            calculate_g2_dists(i, true);
         }
         WriteDists(g2_dists_inner, $"Dists inner n={segmentResolution}.txt");
         WriteDists(g2_dists_outer, $"Dists outer n={segmentResolution}.txt");
      }

      
      //
      inputBuffer.Release();
      outputBufferOuter.Release();
      outputBufferInner.Release();
      absorptionsBuffer.Release();
   }
   
   /**
    * g1 pass: Compute g1 dists in shader buffers
    */
   public void calculate_g1_dists() {
      cs.SetBuffer(g1_handle, "segment", inputBuffer);
      cs.SetBuffer(g1_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(g1_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(g1_handle, 
         (int) Math.Min(Math.Pow(2,16)-1, Math.Pow(segmentResolution, 2)), 
         1, 
         1
         );   // TODO: handle case threadGroupsX > 1024
      outputBufferInner.GetData(g1_dists_inner_precomputed);
      outputBufferOuter.GetData(g1_dists_outer_precomputed);
   }

   /**
    * g2 pass: Add g2 dists to g1 dists in shader buffers
    */
   public void calculate_g2_dists(int i, bool copy) {
      cs.SetBuffer(g2_handle, "segment", inputBuffer);
      cs.SetFloat("cos", (float) Math.Cos((180 - model.get_angles2D()[i]) * Math.PI / 180));
      cs.SetFloat("sin", (float) Math.Sin((180 - model.get_angles2D()[i]) * Math.PI / 180));      
      cs.SetBuffer(g2_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(g2_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(g2_handle, (int) Math.Min(Math.Pow(2,16)-1, Math.Pow(segmentResolution, 2)), 
         1, 1);   // TODO: handle case threadGroupsX > 1024
      if (copy)
      {
         Vector2[] tempInner = new Vector2[data.Length];
         Vector2[] tempOuter = new Vector2[data.Length];
         outputBufferInner.GetData(tempInner); // .add??
         outputBufferOuter.GetData(tempOuter); // .add??
         for (int j = 0; j < tempInner.Length; j++) {
            g2_dists_inner[i,j] = tempInner[j];
            g2_dists_outer[i,j] = tempOuter[j];
         }
      }
   }

   public void calculate_g2_3D_dists(int size, int i) {
      // TODO.
   }

   /**
    * absorptions pass: calculate absorptions from distances in shader buffers
    * (used case chosen in run-method)
    */
   public void calculate_absorptions_2D(int i) {
      cs.SetBuffer(g2_handle, "segment", inputBuffer);
      cs.SetFloat("cos", (float) Math.Cos((180 - model.get_angles2D()[i]) * Math.PI / 180));
      cs.SetFloat("sin", (float) Math.Sin((180 - model.get_angles2D()[i]) * Math.PI / 180));      
      cs.SetBuffer(g2_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(g2_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(g2_handle, 
         (int) Math.Min(Math.Pow(2,16)-1, Math.Pow(segmentResolution, 2)), 
         1, 1);
      
      cs.SetBuffer(absorptions_handle, "segment", inputBuffer);
      cs.SetBuffer(absorptions_handle, "absorptions", absorptionsBuffer);
      cs.SetBuffer(absorptions_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(absorptions_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(absorptions_handle, 
         (int) Math.Min(Math.Pow(2,16)-1, Math.Pow(segmentResolution, 2)), 
         1, 1);
      absorptionsBuffer.GetData(absorptions);
   }

   /**
    * Computes average over all non-zero elements, per axis x,y,z.
    */
   void extractAbsorptionFactor(int j, float tol)
   {
      absorptionFactors[j] = new Vector3(
         absorptions.AsParallel().Select(v => v.x).Where(x => Math.Abs(x) >= tol).Average(),
         absorptions.AsParallel().Select(v => v.y).Where(x => Math.Abs(x) >= tol).Average(),
         absorptions.AsParallel().Select(v => v.z).Where(x => Math.Abs(x) >= tol).Average()
      );
   }

   private void WriteDists(Vector2[,] distsArray, string filename)
   {
      Debug.Log("Writing");
      var (n, m) = (distsArray.GetLength(0), distsArray.GetLength(1));
      
      using (var fileStream = File.Create(Path.Combine("Logs", "Distances2D", filename)))
      using (var buffered = new BufferedStream(fileStream))
      using (var writer = new StreamWriter(buffered))
      {
         writer.Write("{");
         
         var sb = new StringBuilder();
         for (int i = 0; i < n; i++)
         {
            /*Debug.Log(Enumerable.Range(0, m)
               .Select(j => g2_dists_inner[i, j])
               .Count(v => v.x > 0)*1.0f/data.Length
            );*/
            sb.Append("\"" + model.get_angles2D()[i] + "\" : ");   // angle as dict key.
            var row = Enumerable.Range(0, m)
               .Select(j => distsArray[i, j])   
               .ToArray();   // get i-th row.
            var rows = Enumerable.Range(0, segmentResolution)
               .Select(k => 
                  "[" +
                  string.Join(",", row
                     .Skip(k * segmentResolution)
                     .Take(segmentResolution)
                     .Select(v => string.Format("[{0:G},{1:G}]", 
                        v.x.ToString("G", CultureInfo.InvariantCulture),
                        v.y.ToString("G", CultureInfo.InvariantCulture)))
                     .ToArray()) + 
                  "]") 
               .ToArray();   // break up 1D array into 2D array, format each Vector2 inside.
            var row_str = string.Join(",", rows).Replace(" ", "");
            sb.Append("[").Append(row_str).Append("]");   // distances array as dict value.
            if (i + 1 < angleSteps)
               sb.Append(",");
            writer.WriteLine(sb.ToString()
                  .Replace("(", "[")
                  .Replace(")", "]")
               );
            sb.Clear();
         }
         
         writer.Write("}");
      }
      
      // TODO: research JSON serialization.
      //File.WriteAllText(Path.Combine("Logs", "Distances2D", $"Output n={size}.txt"), res_str);
   }

   private void WriteAbsorptionFactors()
   {
      using (FileStream fileStream = File.Create(Path.Combine("Logs", "Absorptions2D", $"Output n={segmentResolution}.txt")))
      using (BufferedStream buffered = new BufferedStream(fileStream))
      using (StreamWriter writer = new StreamWriter(buffered))
      {
         writer.WriteLine(string.Join("\t", "2 theta","A_{s,sc}", "A_{c,sc}", "A_{c,c}"));
         for (int i = 0; i < angleSteps; i++)
         {
            writer.WriteLine(string.Join("\t", model.get_angles2D()[i].ToString("G", CultureInfo.InvariantCulture),
               absorptionFactors[i].x.ToString("G", CultureInfo.InvariantCulture),
               absorptionFactors[i].y.ToString("G", CultureInfo.InvariantCulture),
               absorptionFactors[i].z.ToString("G", CultureInfo.InvariantCulture)));
         }
      }
      //File.WriteAllText(Path.Combine("Logs", "Distances2D", $"Output n={size}.txt"), res_string);
   }

}




































