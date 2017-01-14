﻿using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Drawing;
using System.Reflection;
using ikvm;
using processing.core;
using culebra.behaviors;
using CulebraData;
using CulebraData.Objects;
using CulebraData.Utilities;

namespace CulebraData.Drawing
{
    public class Vizualization
    {
        public void drawPointGraphic(IGH_PreviewArgs args, List<Point3d> particleList)
        {
            foreach (Point3d p in particleList)
                args.Display.DrawPoint(p, System.Drawing.Color.Blue);
        }
        public void drawParticles(IGH_PreviewArgs args, string file, ParticleSystem particleSystem)
        {
            Bitmap bm = new Bitmap(file);
            Rhino.Display.DisplayBitmap dbm = new Rhino.Display.DisplayBitmap(bm);
            args.Display.DrawParticles(particleSystem, dbm);
        }
        public void drawSprites(IGH_PreviewArgs args, string file, List<Point3d> particleList)
        {
            Bitmap bm = new Bitmap(file);
            Rhino.Display.DisplayBitmap dbm = new Rhino.Display.DisplayBitmap(bm);
            Rhino.Display.DisplayBitmapDrawList ddl = new Rhino.Display.DisplayBitmapDrawList();
            ddl.SetPoints(particleList, Color.FromArgb(25, 255, 255, 255));
            args.Display.DrawSprites(dbm, ddl, 2.0f, new Vector3d(0, 0, 1), true);
        }
        public void drawGradientTrails(IGH_PreviewArgs args, string file, List<Point3d> particleList, DataTree<Point3d> particleSet, Color randomColorAction, float minTrailThickness, float maxTrailThickness)
        {
            Color color = args.WireColour;
            for (int i = 0; i < particleSet.BranchCount; i++)
            {
                List<Point3d> ptlist = particleSet.Branch(i);
                //-------DRAW TRAILS AS SEGMENTS WITH CUSTOM STROKE WIDTH---------
                if (ptlist.Count > 0)
                {
                    for (int x = 0; x < ptlist.Count; x++)
                    {
                        if (x != 0)
                        {
                            float stroke = CulebraData.Utilities.Convert.map(x / (1.0f * ptlist.Count), 0.0f, 1.0f, minTrailThickness, maxTrailThickness);
                            float colorValue = CulebraData.Utilities.Convert.map(x / (1.0f * ptlist.Count), 0.0f, 1.0f, 0f, 255.0f);
                            args.Display.DrawLine(ptlist[x - 1], ptlist[x], Color.FromArgb(0, (int)colorValue, 0, 100), (int)stroke);
                        }
                    }
                }
            }
        }
        public void drawPolylineGeometryTrails(List<Point3d> ptList)
        {

        }
        public void drawPolylineTrails(IGH_PreviewArgs args, DataTree<Point3d> particleSet, bool dottedPolyline, int thickness)
        {
            Color color = args.WireColour;
            for (int i = 0; i < particleSet.BranchCount; i++)
            {
                List<Point3d> ptlist = particleSet.Branch(i);
                if (dottedPolyline)
                {
                    args.Display.DrawDottedPolyline(ptlist, Color.FromArgb(0, 255, 0, 255), false);
                }
                else
                {
                    args.Display.DrawPolyline(ptlist, Color.FromArgb(0, 255, 0, 255), thickness);
                }
            }
        }
        public void drawDiscoTrails(IGH_PreviewArgs args, string file, List<Point3d> particleList, DataTree<Point3d> particleSet, Random randomGen, Color randomColorAction, float minTrailThickness, float maxTrailThickness)
        {
            Color color = args.WireColour;
            for (int i = 0; i < particleSet.BranchCount; i++)
            {
                List<Point3d> ptlist = particleSet.Branch(i);
                //-------DRAW TRAILS AS SEGMENTS WITH CUSTOM STROKE WIDTH---------
                randomColorAction = CulebraData.Utilities.Convert.GetRandomColor(randomGen);
                if (ptlist.Count > 0)
                {
                    for (int x = 0; x < ptlist.Count; x++)
                    {
                        if (x != 0)
                        {
                            float stroke = CulebraData.Utilities.Convert.map(x / (1.0f * ptlist.Count), 0.0f, 1.0f, minTrailThickness, maxTrailThickness);
                            args.Display.DrawLine(ptlist[x - 1], ptlist[x], randomColorAction, (int)stroke);
                        }
                    }
                }
            }
        }
    }
}
