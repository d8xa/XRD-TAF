using System;
using System.Collections;
using System.Collections.Generic;
using FoPra.model;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngineInternal;

public class LogicHandler {
   public ComputeShader computeShader;
   public Model model;

   private ComputeBuffer bufferSegments;
   private ComputeBuffer bufferDistances;

   struct segmentStruct {
      private Vector2 segment;
   }
   
   public LogicHandler(Model model) {
      this.model = model;
   }
   
   void runShader() {
      int kernelHandle = computeShader.FindKernel("OuterDistances");
      
      bufferSegments = new ComputeBuffer(1000, 10,ComputeBufferType.Structured);
      
      Vector2 n = new Vector2(1f, -1f/Mathf.Tan(40));
      n.Normalize();
      
      computeShader.SetFloat("r_cell", model.sample.totalDiameter);
      computeShader.SetFloat("r_sample", model.sample.totalDiameter - model.sample.cellThickness);
      computeShader.SetFloat("m", Mathf.Tan(40));
      computeShader.SetFloats("n", new float[2]{n.x,n.y});
      
      //computeShader.SetBuffer();
      
      computeShader.Dispatch(kernelHandle,32,32,1);
   }
   
   
}

