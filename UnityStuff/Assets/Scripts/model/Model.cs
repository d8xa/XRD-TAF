using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FoPra.util;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace FoPra.model
{
  public class Model
  {
    //enum Modes {_1D, _2D}  // to switch between use cases and discard unused fields. TODO: integrate. 
    [Serializable]
    public enum Mode {
      Point, Area, Integrated, Testing
    }
    public enum AbsorptionType {
      All, Cell, Sample, CellAndSample
    }
    public enum RayProfile {
      Oval, Rectangle
    }

    public Settings settings;
    public DetektorSettings detector;
    public SampleSettings sample;
    public RaySettings ray;

    private int segmentResolution;
    private float r_sample;
    private float r_sample_sq;
    private float r_cell;
    private float r_cell_sq;
    private float mu_sample;
    private float mu_cell;
    private float[] angles2D;
    private float[] anglesIntegrated;

    private void calculate_meta_data()
    {
      segmentResolution = (int) settings.computingAccuracy; // TODO: rename text field label in GUI.
      r_sample = sample.totalDiameter / 2 - sample.cellThickness;
      r_sample_sq = (float) Math.Pow(r_sample, 2);
      r_cell = sample.totalDiameter / 2;
      r_cell_sq = (float) Math.Pow(r_cell, 2);
      mu_sample = sample.muSample;
      mu_cell = sample.muCell;
      
      anglesIntegrated = MathTools.LinSpace1D(detector.angleStart, detector.angleEnd, detector.angleAmount);

      var text = "";
      var path = Path.Combine(Application.dataPath, "Input", detector.pathToAngleFile + ".txt");
      if (File.Exists(path))
      {
        using (var reader = new StreamReader(path))
          text = reader.ReadToEnd();
      }


      if (settings.mode.Equals(Mode.Point) || settings.mode.Equals(Mode.Testing))
        angles2D = text
          .Trim(' ')
          .Split('\n')
          .Where(s => s.Length > 0)
          .Select(s => float.Parse(s, CultureInfo.InvariantCulture))
          .ToArray();
      else
        angles2D = Enumerable.Range(0, (int) detector.resolution.x)
          .Select(j => detector.getAngleFromOffset(j, false))
          .ToArray();
    }

    public Model(Settings settings, DetektorSettings detector, SampleSettings sample/*, RaySettings ray*/) {
      this.settings = settings;
      this.detector = detector;
      this.sample = sample;
      this.ray = ray;
      calculate_meta_data();
    }

    public int get_accuracy_resolution_size() => segmentResolution;
    
    public float get_r_sample() => r_sample;

    public float get_r_sample_sq() => r_sample_sq;

    public float get_r_cell() => r_cell;

    public float get_r_cell_sq() => r_cell_sq;

    public float get_mu_sample() => mu_sample;

    public float get_mu_cell() => mu_cell;

    public float[] get_angles2D() => angles2D;

    public float[] get_anglesIntegrated() => anglesIntegrated;
  }
}
