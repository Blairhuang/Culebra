﻿using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Reflection;
using ikvm;
using processing.core;
using culebra.behaviors;
using CulebraData;
using CulebraData.Objects;


namespace Culebra_GH.Tests
{
    public class Component_Test_SelfTailChasers : GH_Component
    {
        //-----------------Global Variables---------------------------
        private List<Vector3d> moveList = new List<Vector3d>();
        private List<Vector3d> startList = new List<Vector3d>();
        private DataTree<Point3d> posTree;

        private BabyCreeper babyCreep;
        private Creeper creep;
        private List<Creeper> creepList = new List<Creeper>();
        private List<Point3d> currentPosList = new List<Point3d>();

        private List<Vector3d> totTail = new List<Vector3d>();

        private Vector3d startPos = new Vector3d();
        private Vector3d moveVec;
        private BoundingBox bb;
        private int dimensions;
        
        /// <summary>
        /// Initializes a new instance of the ComponentTest class.
        /// </summary>
        public Component_Test_SelfTailChasers()
            : base("Component_Test_SelfTailChasers", "Nickname",
                "Description",
                "Culebra_GH", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("reset", "r", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Creeps", "C", "C", GH_ParamAccess.list);
            pManager.AddPointParameter("Trails", "T", "T", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ikvm.runtime.Startup.addBootClassPathAssemby(Assembly.Load("culebra"));
            ikvm.runtime.Startup.addBootClassPathAssemby(Assembly.Load("IKVM.OpenJDK.Core"));

            Random rnd = new Random();
            List<Vector3d> vecs = new List<Vector3d>();
            List<Point3d> returnedvecs = new List<Point3d>();

            bool reset = new bool();
            int ptCount = 95;
            int dimension = 0;
            bool trail = true;

            float moveValue = 3.44f;
            float searchRad = 44.04f;
            float aligVal = 0.04f;
            float sepVal = 0.15f;
            float cohVal = 0.24f;

            if(!DA.GetData(0, ref reset))return;

            if (reset)
            { //we are using the reset to reinitialize all the variables and positions to pass to the class once we are running

                this.moveList = new List<Vector3d>();
                this.startList = new List<Vector3d>();
                this.posTree = new DataTree<Point3d>();
                this.dimensions = dimension;
                creepList = new List<Creeper>();
                currentPosList = new List<Point3d>();

                totTail = new List<Vector3d>();

                if (this.dimensions == 0)
                {
                    this.bb = new BoundingBox(-250, -250, 0, 250, 250, 0);
                }
                else
                {
                    this.bb = new BoundingBox(-250, -250, -250, 250, 250, 500);
                }
                for (int i = 0; i < ptCount; i++)
                {
                    if (this.dimensions == 0)
                    { //IF WE WANT 2D
                        //this.moveVec = new Vector3d(moveValue, 0, 0); //move to the right only
                        //this.startPos = new Vector3d((int)bb.Min[0], rnd.Next((int)bb.Min[1], (int)bb.Max[1]), 0); //spawn along the y axis of the bounding area
                        this.moveVec = new Vector3d(rnd.Next(-1, 2) * 0.5, rnd.Next(-1, 2) * 0.5, 0); //move randomly in any direction 2d
                        this.startPos = new Vector3d(rnd.Next((int)bb.Min[0], (int)bb.Max[0]), rnd.Next((int)bb.Min[1], (int)bb.Max[1]), 0); //spawn randomly inside the bounding area

                        if (i <= ptCount / 2)
                        {
                            this.creep = new Creeper(this.startPos, this.moveVec, true, false);
                            this.creepList.Add(this.creep);
                        }
                        else
                        {
                            this.babyCreep = new BabyCreeper(this.startPos, this.moveVec, true, "a", false);
                            this.creepList.Add(this.babyCreep);
                        }
                        
                    }
                    else
                    { //IF WE WANT 3D
                        this.moveVec = new Vector3d(rnd.Next(-1, 2), rnd.Next(-1, 2), 0.5); //move randomly in the xy axis and up in the z axis
                        this.moveVec *= moveValue;
                        this.startPos = new Vector3d(rnd.Next((int)bb.Min[0], (int)bb.Max[0]), rnd.Next((int)bb.Min[1], (int)bb.Max[1]), (int)bb.Min[2]); //start randomly on the lowest plane of the 3d bounds
                    }
                    this.startList.Add(this.startPos); //add the initial starting positions to the list to pass once we start running
                    this.moveList.Add(this.moveVec); //add the initial move vectors to the list to pass once we start running
                }

                DA.SetDataList(0, this.startList);
            }
            else
            {
                currentPosList = new List<Point3d>();
                DataTree<Point3d> trailTree = new DataTree<Point3d>();

                int counter = 0;
                foreach (Creeper c in this.creepList)
                {
                    
                    c.attributes.setMoveAttributes(3.44f, 0.130f, 1.5f);
                    this.totTail.AddRange(c.attributes.getTrailVectors());
                    c.behaviors.selfTailChase(60.0f, 1.5f, 80.0f, 0.00f, 5.00f, totTail);
                    //c.behaviors.wander2D(true,false,2.0f, 80.0f, 26.0f);
                    //c.behaviors.flock2D(searchRad, cohVal, sepVal, aligVal, 360f, this.creepList, false);                
                    c.actions.move(6,100);
                    c.actions.bounce(bb);
                    currentPosList.Add(c.attributes.getLocation());
                                   
                    GH_Path path = new GH_Path(counter);
                    trailTree.AddRange(c.attributes.getTrailPoints(),path);
                    
                    counter++;
                }
                
                DA.SetDataList(0, currentPosList);
                if (trail)
                {
                    DA.SetDataTree(1, trailTree);
                }
                this.totTail.Clear();
            }
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
            get { return new Guid("{b7bef7b8-4200-4545-853e-a6afe2bc58bb}"); }
        }
    }
}