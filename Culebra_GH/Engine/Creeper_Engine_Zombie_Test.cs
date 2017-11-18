﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Culebra_GH.Objects;
using Grasshopper;
using System.Reflection;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Drawing;
using ikvm;
using processing.core;
using culebra.behaviors;
using CulebraData;
using CulebraData.Objects;
using CulebraData.Utilities;
using CulebraData.Drawing;
using System.Collections;
using System.Linq;
using Culebra_GH.Data_Structures;

namespace Culebra_GH.Engine
{
    public class Creeper_Engine_Zombie_Test : GH_Component
    {
        private List<Vector3d> moveList;
        private List<Vector3d> startList;
        private List<Point3d> ptList;

        private Vector3d startPos = new Vector3d();
        private Vector3d moveVec;
        private BoundingBox bb;
        private int dimensions;

        private bool bounds;
        private Box box;

        private int spawnType;
        private int pointCount;
        private string spawnData;

        private Creeper creep;
        private List<CulebraObject> creepList = new List<CulebraObject>();
        private List<Point3d> currentPosList = new List<Point3d>();
        private List<Line> networkList = new List<Line>();

        private double initialSpeed, maxSpeed, maxForce, velMultiplier;

        private int minthick = new int();
        private int maxthick = new int();

        private bool trail = new bool();
        private int displayMode;

        private int trailStep;
        private int maxTrailSize;

        private string particleTexture = "";

        private double[] redValues = new double[2];
        private double[] greenValues = new double[2];
        private double[] blueValues = new double[2];

        private Color polylineColor;
        private bool dotted;
        private string graphicType;

        /// <summary>
        /// Initializes a new instance of the Creeper_Engine class.
        /// </summary>
        public Creeper_Engine_Zombie_Test()
          : base("Creeper_ZombieEngine_Test", "CZE",
              "Engine Module Test Viz",
              "Culebra_GH", "04 | Engine")
        {
        }
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Init Settings", "S", "Input the init settings output of Init component", GH_ParamAccess.list);
            pManager.AddGenericParameter("Move Settings", "S", "Input the move settings output from the Move component", GH_ParamAccess.list);
            pManager.AddGenericParameter("Behavioral Settings", "BS", "Input the behavior settings output from the Controller component", GH_ParamAccess.item);
            pManager.AddGenericParameter("Visual Settings", "VS", "Input the visual settings output of Viz component", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Iterations", "I", "Input the number of steps you would like to run", GH_ParamAccess.item);
            pManager.AddGenericParameter("Run", "R", "Input a button to reset the sim and reset all fields", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Creepers", "C", "Outputs the heads of the creepers", GH_ParamAccess.list);
            pManager.AddGenericParameter("Trails", "T", "Outputs data trees for each Creeper with its trail polyline", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Connectivity", "CN", "Outputs curves connecting from creeper heads which indicate their search rad", GH_ParamAccess.list);
            pManager.AddGenericParameter("BoundingBox", "BB", "Outputs the working bounds of the sim", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ikvm.runtime.Startup.addBootClassPathAssemby(Assembly.Load("culebra"));
            ikvm.runtime.Startup.addBootClassPathAssemby(Assembly.Load("IKVM.OpenJDK.Core"));

            bool reset = new bool();
            int iterations = new int();

            List<object> init_Settings = new List<object>();
            List<object> move_Settings = new List<object>();
            IGH_VisualData visual_Settings = null;

            object behavioral_Settings = null;

            if (!DA.GetDataList(0, init_Settings) || init_Settings.Count == 0 || init_Settings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Init Settings Detected, please connect Init Settings to enable the component");
                return;
            }
            if (!DA.GetDataList(1, move_Settings) || move_Settings.Count == 0 || move_Settings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Move Settings Detected, please connect Move Settings to enable the component");
                return;
            }
            if (!DA.GetData(3, ref visual_Settings) || visual_Settings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Visual Settings Detected, please connect Visual Settings to enable the component");
                return;
            }
            if (!DA.GetData(4, ref iterations)) return;
            if (!DA.GetData(5, ref reset)) return;
            Random rnd = new Random();
            if (!DA.GetData(2, ref behavioral_Settings) || behavioral_Settings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input Object is Null");
                return;
            }
            string objtype = behavioral_Settings.GetType().Name.ToString();
            if (!(behavioral_Settings.GetType() == typeof(IGH_BehaviorData)))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You did not input a Behavior Data Object, please ensure input is Behavior Data Object and not " + objtype);
                return;
            }
            else
            {
                //------------------------Init Settings--------------------------
                if (init_Settings.Count != 0)
                {
                    String init_Convert = "";
                    //GH_Convert.ToString(init_Settings[0], out init_Convert, GH_Conversion.Primary);
                    if (init_Settings[0].GetType() == typeof(GH_String))
                    {
                        GH_String value = (GH_String)init_Settings[0];
                        init_Convert = value.Value;
                    }

                    if (init_Convert == "Box")
                    {
                        this.spawnData = "box";
                        GH_Convert.ToBox_Primary(init_Settings[3], ref this.box);
                        GH_Convert.ToInt32(init_Settings[4], out this.spawnType, GH_Conversion.Primary);
                        GH_Convert.ToInt32(init_Settings[5], out this.pointCount, GH_Conversion.Primary);
                        GH_Convert.ToInt32(init_Settings[1], out this.dimensions, GH_Conversion.Primary);

                    }
                    else if (init_Convert == "Points")
                    {
                        this.spawnData = "Points";
                        var wrapperToGoo = GH_Convert.ToGoo(init_Settings[3]);
                        wrapperToGoo.CastTo<List<Point3d>>(out this.ptList);
                        GH_Convert.ToInt32(init_Settings[1], out this.dimensions, GH_Conversion.Primary);
                    }
                    GH_Convert.ToBoolean(init_Settings[2], out this.bounds, GH_Conversion.Primary);
                }
                //------------------------Move Settings--------------------------
                if (move_Settings.Count != 0)
                {
                    GH_Convert.ToDouble(move_Settings[0], out this.initialSpeed, GH_Conversion.Primary);
                    GH_Convert.ToDouble(move_Settings[1], out this.maxSpeed, GH_Conversion.Primary);
                    GH_Convert.ToDouble(move_Settings[2], out this.maxForce, GH_Conversion.Primary);
                    GH_Convert.ToDouble(move_Settings[3], out this.velMultiplier, GH_Conversion.Primary);
                }
                //------------------------Visual Settings--------------------------
                TrailData td = visual_Settings.Value.trailData;
                ColorData cd = visual_Settings.Value.colorData;
                this.trail = td.createTrail;
                this.displayMode = visual_Settings.Value.displayMode;
                this.trailStep = td.trailStep;
                this.maxTrailSize = td.maxTrailSize;
                this.particleTexture = cd.particleTexture;
                this.graphicType = cd.colorDataType;
                if (cd.colorDataType == "Gradient")
                {
                    this.maxthick = cd.maxThickness;
                    this.minthick = cd.minThickness;
                    this.redValues[0] = cd.redChannel[0];
                    this.redValues[1] = cd.redChannel[1];
                    this.greenValues[0] = cd.greenChannel[0];
                    this.greenValues[1] = cd.greenChannel[1];
                    this.blueValues[0] = cd.blueChannel[0];
                    this.blueValues[1] = cd.blueChannel[1];
                }
                else if (cd.colorDataType == "GraphicPolyline")
                {
                    this.polylineColor = cd.color;
                    this.dotted = cd.dotted;
                    this.maxthick = cd.maxThickness;
                }
                else if (cd.colorDataType == "Disco")
                {
                    this.maxthick = cd.maxThickness;
                    this.minthick = cd.minThickness;
                }
                //-----------------------------------------------------------------
                this.bb = new BoundingBox();
                int loopCount = new int();
                bool create = new bool();
                if (this.spawnData == "box")
                {
                    this.bb = this.box.BoundingBox;
                    loopCount = this.pointCount;
                    create = true;
                }
                else if (this.spawnData == "Points")
                {
                    loopCount = this.ptList.Count;
                    create = false;
                    this.bounds = false;
                }
                //------------------------RESET STARTS HERE--------------------------
                this.moveList = new List<Vector3d>();
                this.startList = new List<Vector3d>();
                this.creepList = new List<CulebraObject>();
                this.currentPosList = new List<Point3d>();
                this.networkList = new List<Line>();
                if (reset)
                {
                    for (int i = 0; i < loopCount; i++)
                    {
                        if (this.dimensions == 0)
                        { //IF WE WANT 2D
                            setViewport("Top", "Shaded");
                            if (create)
                            {
                                if (this.spawnType == 0 || this.spawnType == 2)
                                {
                                    this.startPos = new Vector3d((int)bb.Min[0], rnd.Next((int)bb.Min[1], (int)bb.Max[1]), 0); //spawn along the y axis of the bounding area
                                }
                                else if (this.spawnType == 1 || this.spawnType == 3)
                                {
                                    this.startPos = new Vector3d(rnd.Next((int)bb.Min[0], (int)bb.Max[0]), rnd.Next((int)bb.Min[1], (int)bb.Max[1]), 0); //spawn randomly inside the bounding area
                                }
                                //this.moveVec = new Vector3d(moveValue, 0, 0); //move to the right only   
                                this.moveVec = new Vector3d(rnd.Next(-1, 2) * initialSpeed, rnd.Next(-1, 2) * initialSpeed, 0); //move randomly in any direction 2d 
                            }
                            else
                            {
                                this.startPos = (Vector3d)this.ptList[i];
                                this.bb.Union(this.ptList[i]);
                                //this.moveVec = new Vector3d(moveValue, 0, 0); //move to the right only   
                                this.moveVec = new Vector3d(rnd.Next(-1, 2) * initialSpeed, rnd.Next(-1, 2) * initialSpeed, 0); //move randomly in any direction 2d                        
                            }
                            this.creep = new Creeper(this.startPos, this.moveVec, true, false);
                            this.creepList.Add(this.creep);
                        }
                        else
                        {
                            setViewport("Perspective", "Shaded");
                            if (create)
                            {
                                if (this.spawnType == 0 || this.spawnType == 2)
                                {
                                    this.startPos = new Vector3d(rnd.Next((int)bb.Min[0], (int)bb.Max[0]), rnd.Next((int)bb.Min[1], (int)bb.Max[1]), (int)bb.Min[2]); //start randomly on the lowest plane of the 3d bounds
                                    this.moveVec = new Vector3d(rnd.Next(-2, 2) * initialSpeed, rnd.Next(-2, 2) * initialSpeed, 1 * initialSpeed); //move randomly in the xy axis and up in the z axis
                                }
                                else if (this.spawnType == 1 || this.spawnType == 3)
                                {
                                    this.startPos = new Vector3d(rnd.Next((int)bb.Min[0], (int)bb.Max[0]), rnd.Next((int)bb.Min[1], (int)bb.Max[1]), rnd.Next((int)bb.Min[2], (int)bb.Max[2])); //start randomly inside the 3d bounds
                                    this.moveVec = new Vector3d(rnd.Next(-2, 2) * initialSpeed, rnd.Next(-2, 2) * initialSpeed, rnd.Next(-2, 2) * initialSpeed); //move randomly in any direction 3d
                                }
                            }
                            else
                            {
                                this.startPos = (Vector3d)this.ptList[i];
                                this.bb.Union(this.ptList[i]);
                                //this.moveVec = new Vector3d(moveValue, 0, 0); //move to the right only   
                                this.moveVec = new Vector3d(rnd.Next(-2, 2) * initialSpeed, rnd.Next(-2, 2) * initialSpeed, rnd.Next(-2, 2) * initialSpeed); //move randomly in any direction 3d
                            }
                            this.creep = new Creeper(this.startPos, this.moveVec, true, true);
                            this.creepList.Add(this.creep);
                        }
                        this.startList.Add(this.startPos); //add the initial starting positions to the list to pass once we start running
                        this.moveList.Add(this.moveVec); //add the initial move vectors to the list to pass once we start running
                    }
                    DA.SetDataList(0, this.startList);

                    DataTree<Point3d> trailTree = new DataTree<Point3d>();
                    for (int z = 0; z < iterations; z++)
                    {
                        trailTree = new DataTree<Point3d>();
                        this.particleSet = new DataTree<Point3d>();
                        this.currentPosList = new List<Point3d>();
                        DataTree<Line> networkTree = new DataTree<Line>();
                        if (this.moveList == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please Reset the CreepyCrawlers Component"); return; }
                        int counter = 0;
                        foreach (Creeper c in this.creepList)
                        {
                            this.networkList = new List<Line>();
                            c.attributes.SetMoveAttributes((float)maxSpeed, (float)maxForce, (float)velMultiplier);
                            IGH_BehaviorData igh_Behavior = (IGH_BehaviorData)behavioral_Settings;
                            foreach (string s in igh_Behavior.Value.dataOrder)
                            {
                                if (s == "Flocking")
                                {
                                    if (this.dimensions == 0)
                                    {
                                        c.behaviors.Flock2D((float)igh_Behavior.Value.flockData.searchRadius, (float)igh_Behavior.Value.flockData.cohesion_Value, (float)igh_Behavior.Value.flockData.separation_Value, (float)igh_Behavior.Value.flockData.alignment_Value, (float)igh_Behavior.Value.flockData.viewAngle, this.creepList, igh_Behavior.Value.flockData.network);
                                    }
                                    else if (this.dimensions == 1)
                                    {
                                        c.behaviors.Flock3D((float)igh_Behavior.Value.flockData.searchRadius, (float)igh_Behavior.Value.flockData.cohesion_Value, (float)igh_Behavior.Value.flockData.separation_Value, (float)igh_Behavior.Value.flockData.alignment_Value, (float)igh_Behavior.Value.flockData.viewAngle, this.creepList, igh_Behavior.Value.flockData.network);
                                    }
                                }
                                else if (s == "Wandering")
                                {
                                    if (this.dimensions == 0)
                                    {
                                        c.behaviors.Wander2D(igh_Behavior.Value.wanderData.randomize, igh_Behavior.Value.wanderData.addHeading, (float)igh_Behavior.Value.wanderData.change, (float)igh_Behavior.Value.wanderData.wanderingRadius, (float)igh_Behavior.Value.wanderData.wanderingDistance);
                                    }
                                    else if (this.dimensions == 1)
                                    {
                                        c.behaviors.Wander3D((float)igh_Behavior.Value.wanderData.change, (float)igh_Behavior.Value.wanderData.wanderingRadius, (float)igh_Behavior.Value.wanderData.wanderingDistance, (float)igh_Behavior.Value.wanderData.rotationTrigger);
                                    }
                                }
                                else if (s == "Tracking")
                                {
                                    c.behaviors.MultiPolylineTracker(igh_Behavior.Value.trackingData.polylines, igh_Behavior.Value.trackingData.pathThreshold, igh_Behavior.Value.trackingData.projectionDistance, igh_Behavior.Value.trackingData.pathRadius);
                                }
                                else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Houston we have a problem, no behavior data read"); return; }
                            }
                            GH_Path path = new GH_Path(counter);
                            if (this.displayMode == 0)
                            {
                                particleList.Add(c.attributes.GetLocation());
                                this.particleSet.AddRange(c.attributes.GetTrailPoints(), path);
                            }
                            if (this.displayMode == 1)
                            {
                                List<Vector3d> testList = c.attributes.GetNetwork();
                                if (testList.Count > 0)
                                {
                                    foreach (Vector3d v in testList)
                                    {
                                        Line l = new Line(c.attributes.GetLocation(), (Point3d)v);
                                        networkList.Add(l);
                                    }
                                    networkTree.AddRange(networkList, path);
                                }
                            }
                            c.actions.Move(this.trailStep, this.maxTrailSize);
                            if (this.bounds)
                            {
                                if (this.dimensions == 0) { c.actions.Bounce(bb); }
                                else if (this.dimensions == 1) { c.actions.Bounce3D(bb); }
                            }
                            this.currentPosList.Add(c.attributes.GetLocation());
                            if (this.displayMode == 1)
                            {
                                if (this.trail) { trailTree.AddRange(c.attributes.GetTrailPoints(), path); }
                            }
                            counter++;
                        }
                    }
                    DA.SetDataList(0, this.currentPosList);
                    if (this.displayMode == 1)
                    {
                        if (this.trail) { DA.SetDataTree(1, trailTree); }
                    }
                }
            }
        }

        public List<Point3d> particleList = new List<Point3d>();
        public DataTree<Point3d> particleSet = new DataTree<Point3d>();

        public Random randomGen = new Random();
        public Color randomColorAction = new Color();
        private BoundingBox _clippingBox;

        public Vizualization viz = new Vizualization();

        protected override void BeforeSolveInstance()
        {
            if (this.displayMode == 0)
            {
                this.particleList.Clear();
                this.particleSet.Clear();
                _clippingBox = BoundingBox.Empty;
            }
        }
        protected override void AfterSolveInstance()
        {
            if (this.displayMode == 0)
            {
                _clippingBox = new BoundingBox(particleList);
            }
        }
        public override BoundingBox ClippingBox
        {
            get
            {
                return _clippingBox;
            }
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (this.displayMode == 0)
            {
                if (this.particleTexture == string.Empty)
                {
                    this.particleTexture = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + @"\Grasshopper\Libraries\Culebra_GH\textures\texture.png";
                }

                //viz.DrawSprites(args, particleTexture, particleList);
                //viz.DrawDiscoTrails(args, file, particleSet, randomGen, this.minthick, this.maxthick);
                /*
                viz.DrawGradientTrails(args, file, particleSet, 0, this.minthick, this.maxthick);
                viz.DrawGradientTrails(args, file, seeker_particleSet, 1, this.minthick, this.maxthick);

                viz.DrawGradientTrails(args, file, particleBabyASet, 1, this.minthick, this.maxthick);
                viz.DrawGradientTrails(args, file, particleBabyBSet, 2, this.minthick, this.maxthick);
                */
                if (this.trail)
                {
                    if (this.graphicType == "Gradient")
                    {
                        viz.DrawGradientTrails(args, particleSet, (float)this.redValues[0], (float)this.redValues[1], (float)this.greenValues[0], (float)this.greenValues[1], (float)this.blueValues[0], (float)this.blueValues[1], this.minthick, this.maxthick);
                    }
                    else if (this.graphicType == "GraphicPolyline")
                    {
                        viz.DrawPolylineTrails(args, particleSet, this.dotted, this.maxthick, this.polylineColor);
                    }
                    else if (this.graphicType == "Disco")
                    {
                        viz.DrawDiscoTrails(args, particleSet, randomGen, this.minthick, this.maxthick);
                    }
                }
            }
        }
        private void setViewport(String view, String displayType)
        {
            //-------Set viewport to specific view and maximize it & set display type--------
            Rhino.DocObjects.Tables.NamedViewTable nvt = Rhino.RhinoDoc.ActiveDoc.NamedViews;
            Rhino.DocObjects.Tables.ViewTable vt = Rhino.RhinoDoc.ActiveDoc.Views;
            Rhino.Display.RhinoView[] rvs = vt.GetViewList(true, false);

            List<Rhino.Display.RhinoView> st = rvs.ToList();
            List<String> viewList = new List<String>();
            int viewCount = 0;
            foreach (Rhino.Display.RhinoView v in st)
            {
                if (v.ActiveViewport.Name == view)
                {
                    Rhino.Display.RhinoViewport rvp = rvs[viewCount].ActiveViewport;
                    rvs[viewCount].Maximized = true;
                }
                viewCount++;
            }
            var type = Rhino.Display.DisplayModeDescription.FindByName(displayType);
            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.DisplayMode = type;
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
            get { return new Guid("d86c09e4-a71e-4550-8290-87f3644c8ecb"); }
        }
    }
}