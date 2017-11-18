﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Collections;
using Grasshopper.Kernel.Types;
using Culebra_GH.Data_Structures;

namespace Culebra_GH.Settings_Visual
{
    public class Disco_Color : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Disco_Color class.
        /// </summary>
        public Disco_Color()
          : base("Disco_Color", "DC",
              "Controls the Disco Color trail options for the Visual Settings Component",
              "Culebra_GH", "05 | Display")
        {
        }
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.tertiary;
            }
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Particle Texture Path", "PT", "Input path to particle texture, a png file", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Minimum Thickness", "NT", "Input the polyline minimum thickness", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Maximum Thickness", "MT", "Input the polyline maximum thickness", GH_ParamAccess.item);

            pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Trail Color Data", "TCD", "Outputs the Trail Color Data for the Visual Settings Component", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = "";
            int minThickness = new int();
            int maxThickness = new int();

            DA.GetData(0, ref path);
            if (!DA.GetData(1, ref minThickness)) return;
            if (!DA.GetData(2, ref maxThickness)) return;

            ColorData colorData = new ColorData();
            colorData.particleTexture = path;
            colorData.maxThickness = maxThickness;
            colorData.minThickness = minThickness;

            colorData.colorDataType = "Disco";

            DA.SetData(0, colorData);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("92bb2582-52e7-40c5-8fa8-1a551a4d59b3"); }
        }
    }
}