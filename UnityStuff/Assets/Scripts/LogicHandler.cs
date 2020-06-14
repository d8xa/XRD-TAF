using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using model;
using UnityEngine;
using util;
using Debug = UnityEngine.Debug;

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
   private Vector3[,] absorptionFactors3D;

   private int g1_handle;
   private int g2_handle;
   private int absorptions2dHandle;
   private int absorptions3dHandle;

   
   private ComputeBuffer inputBuffer;
   private ComputeBuffer outputBufferOuter;
   private ComputeBuffer outputBufferInner;
   private ComputeBuffer absorptionsBuffer;
   
   public LogicHandler(Model model, ComputeShader cs) {
      this.model = model;
      this.cs = cs;
      segmentResolution = model.GetSegmentResolution();
   }

   /**
    * Data generation,
    * - defines empty Vector2[] g1_dists for pre-calculation purposes;
    * - defines empty Vector3[] absorptions for absorptions per ???;
    * - sets handles
    */
   void initDataFields(int segmentRes) {
      data = MathTools.LinSpace2D(-model.GetRCell()*margin, model.GetRCell()*margin, segmentRes);
      Debug.Log(data.Length);
      
      var sw = new Stopwatch();
      // TODO: maybe use length of angle list directly instead.
      if (model.settings.mode == Model.Mode.Point)
         angleSteps = model.GetAngles2D().Length;
      else if (model.settings.mode == Model.Mode.Area)
      {
         // TODO: error checking.
         angleSteps = (int) model.detector.resolution.x;
         absorptionFactors3D = new Vector3[(int) model.detector.resolution.x, (int) model.detector.resolution.y];
      }
      else if (model.settings.mode == Model.Mode.Integrated)
      {
         // TODO:
      }
      else
         angleSteps = model.GetAngles2D().Length;
      
      Debug.Log($"{sw.Elapsed}: Strating Initialized g1 distance arrays.");
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
      
      cs.SetFloats("mu", model.GetMuCell(), model.GetMuSample());
      cs.SetFloat("r_cell", model.GetRCell());
      cs.SetFloat("r_sample", model.GetRSample());
      cs.SetFloat("r_cell_sq", model.GetRCellSq());
      cs.SetFloat("r_sample_sq", model.GetRSampleSq());
      cs.SetFloat("cos_alpha", 1.0f);

      g1_handle = cs.FindKernel("g1_dists");
      g2_handle = cs.FindKernel("g2_dists");
      absorptions2dHandle = cs.FindKernel("Absorptions2D");
      absorptions3dHandle = cs.FindKernel("Absorptions3D");
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
         Stopwatch sw = new Stopwatch();
         sw.Start();
         for (int j = 0; j < angleSteps; j++) {
            outputBufferInner.SetData(g1_dists_inner_precomputed);
            outputBufferOuter.SetData(g1_dists_outer_precomputed);
            calculate_absorptions_2D(j);
            absorptionFactors[j] = GetAbsorptionFactor();
         }
         Debug.Log(sw.Elapsed.ToString("G"));
         sw.Stop();
         //WriteAbsorptionFactors();
      } 
      else if (model.settings.mode == Model.Mode.Area) 
      {
         var stopwatch = new Stopwatch();
         stopwatch.Start();
         for (int j = 0; j < angleSteps; j++)
         {
            if ((j % 32) == 0)
               Debug.Log($"Detector absorption factor, iteration={j.ToString()}");
            outputBufferInner.SetData(g1_dists_inner_precomputed);
            outputBufferOuter.SetData(g1_dists_outer_precomputed);
            calculate_g2_dists(j, true);
            calculateAbsorptions3D_column(j);
         }
         stopwatch.Stop();
         var ts = stopwatch.Elapsed;
         Debug.Log($"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");
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
      cs.SetFloat("cos", (float) Math.Cos((180 - model.GetAngles2D()[i]) * Math.PI / 180));
      cs.SetFloat("sin", (float) Math.Sin((180 - model.GetAngles2D()[i]) * Math.PI / 180));      
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

   public void calculate_absorptions_2D(int i) {
      cs.SetBuffer(g2_handle, "segment", inputBuffer);
      cs.SetFloat("cos", (float) Math.Cos((180 - model.GetAngles2D()[i]) * Math.PI / 180));
      cs.SetFloat("sin", (float) Math.Sin((180 - model.GetAngles2D()[i]) * Math.PI / 180));      
      cs.SetBuffer(g2_handle, "distancesInner", outputBufferInner);
      cs.SetBuffer(g2_handle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(g2_handle, 
         (int) Math.Min(Math.Pow(2,16)-1, Math.Pow(segmentResolution, 2)), 
         1, 1);
      
      cs.SetBuffer(absorptions2dHandle, "segment", inputBuffer);
      cs.SetBuffer(absorptions2dHandle, "absorptions", absorptionsBuffer);
      cs.SetBuffer(absorptions2dHandle, "distancesInner", outputBufferInner);
      cs.SetBuffer(absorptions2dHandle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(absorptions2dHandle, 
         (int) Math.Min(Math.Pow(2,16)-1, Math.Pow(segmentResolution, 2)), 
         1, 1);
      absorptionsBuffer.GetData(absorptions);
   }

   public void calculateAbsorptions3D_column(int j)
   {
      double deltaX = Math.Abs(j * model.detector.pixelsize - model.detector.offSetFromDownRightEdge.x);
      double b = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(model.detector.distToSample, 2));
            
      for (int i = 0; i < angleSteps; i++)
      {
         double deltaY = Math.Abs(i*model.detector.pixelsize - model.detector.offSetFromDownRightEdge.y);
         double c = Math.Sqrt(Math.Pow(b, 2) + Math.Pow(deltaY, 2));
         
         cs.SetBuffer(absorptions3dHandle, "segment", inputBuffer);
         calculateAbsorptions3D_point(cosAlpha: b/c);
         absorptionFactors3D[i,j] = GetAbsorptionFactorLINQ((float) 1E-14);
      }
   }
   
   public void calculateAbsorptions3D_point(double cosAlpha) {
      cs.SetFloat("cos_alpha", (float) cosAlpha);
      cs.SetBuffer(absorptions3dHandle, "absorptions", absorptionsBuffer);
      cs.SetBuffer(absorptions3dHandle, "distancesInner", outputBufferInner);
      cs.SetBuffer(absorptions3dHandle, "distancesOuter", outputBufferOuter);
      cs.Dispatch(absorptions3dHandle, 
         (int) Math.Min(Math.Pow(2,16)-1, Math.Pow(segmentResolution, 2)), 
         1, 1);
      absorptionsBuffer.GetData(absorptions);
   }
   
   private Vector3 GetAbsorptionFactor()
   {
      float[] absorptionSum = new float[]{0.0f, 0.0f, 0.0f};
      uint[] count = new uint[]{0,0,0};

      for (int i = 0; i < data.Length; i++)
      {
         double norm = Vector3.Magnitude(data[i]);
         if (norm <= model.GetRCell())
         {
            if (norm > model.GetRSample())
            {
               absorptionSum[1] += absorptions[i].y;
               absorptionSum[2] += absorptions[i].z;
               count[1] += 1;
               count[2] += 1;
            }
            else
            {
               absorptionSum[0] += absorptions[i].x;
               count[0] += 1;
            }
         }
      }

      for (int i = 0; i < 3; i++)
      {
         if (count[i] == 0) count[i] = 1;
      }

      return new Vector3(
         absorptionSum[0] / count[0],
         absorptionSum[1] / count[1],
         absorptionSum[2] / count[2]
      );
   }

   /**
    * Computes average over all non-zero elements, per axis x,y,z.
    */
   Vector3 GetAbsorptionFactorLINQ(float tol)
   {
       return new Vector3(
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
            sb.Append("\"" + model.GetAngles2D()[i] + "\" : ");   // angle as dict key.
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
            writer.WriteLine(string.Join("\t", model.GetAngles2D()[i].ToString("G", CultureInfo.InvariantCulture),
               absorptionFactors[i].x.ToString("G", CultureInfo.InvariantCulture),
               absorptionFactors[i].y.ToString("G", CultureInfo.InvariantCulture),
               absorptionFactors[i].z.ToString("G", CultureInfo.InvariantCulture)));
         }
      }
      //File.WriteAllText(Path.Combine("Logs", "Distances2D", $"Output n={size}.txt"), res_string);
   }

}




































