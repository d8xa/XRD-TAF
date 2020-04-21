using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace FoPra.model
{
  public class Model
  {
    //enum Modes {_1D, _2D}  // to switch between use cases and discard unused fields. TODO: integrate. 
    public enum Modes {
      Point,Area,Integrated
    }
    public enum AbsorbtionType {
      All,Cell,Sample,CellAndSample
    }
    public enum StrahlProfil {
      Oval,Rechteck
    }

    public Settings settings;
    public DetektorSettings detector;
    public SampleSettings sample;
    public RaySettings ray;

    private int accuracy_resolution_size;
    private float r_sample;
    private float r_sample_sq;
    private float r_cell;
    private float r_cell_sq;
    private float mu_sample;
    private float mu_cell;
    private float[] angles2D;
    private List<float> anglesIntegrated;

    private void calculate_meta_data() {
      accuracy_resolution_size = (int) Math.Pow(2, settings.computingAccuracy);
      r_sample = (sample.totalDiameter - sample.cellThickness) / 2;
      r_sample_sq = (float) Math.Pow(r_sample, 2);
      r_cell = sample.totalDiameter / 2;
      r_cell_sq = (float) Math.Pow(r_cell, 2);
      mu_sample = sample.muSample;
      mu_cell = sample.muCell;
      
      Debug.Log(r_cell.ToString() + ", " + r_sample.ToString());


//      float stepsize = (detector.angleEnd - detector.angleStart)/(detector.angleAmount-1); // -1, damit in der for-Schleife einschliesslich der Grenze gerechnet werden kann
//      for (float i = detector.angleStart; i <= detector.angleEnd; i+=stepsize) {
//        anglesIntegrated.Add(i);
//      }

      string text = "0.0";
      /* TODO: read angle-File vernuenftig implementieren */
      if (File.Exists(Application.dataPath + "/Input/" + detector.pathToAngleFile + ".txt")) {
        StreamReader reader = new StreamReader(Application.dataPath + "/Input/" + detector.pathToAngleFile + ".txt");
        text = reader.ReadToEnd();
        
      }
      string[] textArray = text.Split(';');
      angles2D = new float[textArray.Length];
      for (int i = 0; i < textArray.Length; i++) {
        angles2D[i] = float.Parse(textArray[i], CultureInfo.InvariantCulture.NumberFormat);

      }
      


    }

    public Model(Settings settings, DetektorSettings detector, SampleSettings sample/*, RaySettings ray*/) {
      this.settings = settings;
      this.detector = detector;
      this.sample = sample;
      this.ray = ray;
      calculate_meta_data();
    }

    public int get_accuracy_resolution_size() {
      return accuracy_resolution_size;
    }
    public float get_r_sample() {
      return r_sample;
    }
    public float get_r_sample_sq() {
      return r_sample_sq;
    }
    public float get_r_cell() {
      return r_cell;
    }
    public float get_r_cell_sq() {
      return r_cell_sq;
    }
    public float get_mu_sample() {
      return mu_sample;
    }
    public float get_mu_cell() {
      return mu_cell;
    }
    public float[] get_angles2D() {
      return angles2D;
    }
    public List<float> get_anglesIntegrated() {
      return anglesIntegrated;
    }
  }
}
