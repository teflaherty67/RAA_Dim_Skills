using Autodesk.Revit.UI.Selection;

namespace RAA_Dim_Skills
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Revit application and document variables
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document curDoc = uidoc.Document;

            // prompt user to select wall to dimension
            Reference pickedRef = uiapp.ActiveUIDocument.Selection.PickObject(ObjectType.Element, "Select a wall to dimension");
            Element selectedElem = curDoc.GetElement(pickedRef);

            // check if selected element is a wall
            if (selectedElem is Wall)
            {
                // cast the element as a wall
                Wall selectedWall = selectedElem as Wall;

                // create a reference array to hold the references for dimensioning
                ReferenceArray refArray = new ReferenceArray();

                // create 2 intial references for the wall edges
                Reference wallRef1 = null, wallref2 = null;

                // get the face of the wall
                Face wallFace = GetFace(selectedWall, selectedWall.Orientation);

                // create an array of edge arrays
                EdgeArrayArray edgeArrays = wallFace.EdgeLoops;

                // create an edge array of the overall wall edges
                EdgeArray wallEdges = edgeArrays.get_Item(0);

                // create a list to hold the edges
                List<Edge> listEdges = new List<Edge>();

                // loop through the edges
                foreach (Edge curEdge in wallEdges)
                {
                    // cast the edge as a line
                    Line line = curEdge.AsCurve() as Line;

                    // check if line is vertical
                    if (IsLineVertical(line) == true)
                    {
                        // add to edge list
                        listEdges.Add(curEdge);
                    }
                }

                // order the edges in descending order
                List<Edge> sortedEdges = listEdges.OrderByDescending(e => e.AsCurve().Length).ToList();

                // assign values to the references
                wallRef1 = sortedEdges[0].Reference;
                wallref2 = sortedEdges[1].Reference;

                // add first wall reference to reference array
                refArray.Append(wallRef1);

                // create a category list for doors and windows
                List<BuiltInCategory> catList = new List<BuiltInCategory>() { BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows };

                // create an Element Multicategory Filter
                ElementMulticategoryFilter catFilter = new ElementMulticategoryFilter(catList);

                // get doors and windows from wall & create references
                List<ElementId> wallElemIds = selectedWall.GetDependentElements(catFilter).ToList();

                // loop through each element id
                foreach (ElementId curElemId in wallElemIds)
                {
                    // cast the element Id as an element (Family Instance)
                    FamilyInstance curFI = curDoc.GetElement(curElemId) as FamilyInstance;

                    // create a referecne for the family instance
                    Reference curRef = GetSpecialFamilyReference(curFI, SpecialReferenceType.CenterLR);

                    // add the reference to the reference array
                    refArray.Append(curRef);
                }

                // add second wall reference to reference array
                refArray.Append(wallref2);

                // get wall location curve
                LocationCurve wallLoc = selectedWall.Location as LocationCurve;

                // cast as a line
                Line wallLine = wallLoc.Curve as Line;

                // create some location points for the offset
                XYZ offset1 = GetOffsetByWallOrientation(wallLine.GetEndPoint(0), selectedWall.Orientation,5);
                XYZ offset2 = GetOffsetByWallOrientation(wallLine.GetEndPoint(1), selectedWall.Orientation, 5);

                // create the dimension line
                Line dimLine = Line.CreateBound(offset1, offset2);

                // create a transaction to create the dimension
                using (Transaction t = new Transaction(curDoc))
                {
                    // start the transaction
                    t.Start("Create Dimensions");

                    // create the dimension
                    Dimension newDim = curDoc.Create.NewDimension(uidoc.ActiveView, dimLine, refArray);

                    // commit the transaction
                    t.Commit();
                }
            }
            else
            {
                // if not a wall, display message and exit
                TaskDialog.Show("Error", "The selected element is not a wall.");
                return Result.Failed;
            }

                return Result.Succeeded;
        }

        private XYZ GetOffsetByWallOrientation(XYZ point, XYZ orientation, int value)
        {
           // create new vector
           XYZ newVector = orientation.Multiply(value);

           // create return point
           XYZ returnPoint = point.Add(newVector);

           return returnPoint;
        }

        public enum SpecialReferenceType
        {
            Left = 0,
            CenterLR = 1,
            Right = 2,
            Front = 3,
            CenterFB = 4,
            Back = 5,
            Bottom = 6,
            CenterElevation = 7,
            Top = 8
        }

        private Reference GetSpecialFamilyReference(FamilyInstance inst, SpecialReferenceType refType)
        {
            // source for this method: https://thebuildingcoder.typepad.com/blog/2016/04/stable-reference-string-magic-voodoo.html

            Reference indexRef = null;

            int idx = (int)refType;

            if (inst != null)
            {
                Document dbDoc = inst.Document;

                Options geomOptions = new Options();
                geomOptions.ComputeReferences = true;
                geomOptions.DetailLevel = ViewDetailLevel.Undefined;
                geomOptions.IncludeNonVisibleObjects = true;

                GeometryElement gElement = inst.get_Geometry(geomOptions);
                GeometryInstance gInst = gElement.First() as GeometryInstance;

                String sampleStableRef = null;

                if (gInst != null)
                {
                    GeometryElement gSymbol = gInst.GetSymbolGeometry();

                    if (gSymbol != null)
                    {
                        foreach (GeometryObject geomObj in gSymbol)
                        {
                            if (geomObj is Solid)
                            {
                                Solid solid = geomObj as Solid;

                                if (solid.Faces.Size > 0)
                                {
                                    Face face = solid.Faces.get_Item(0);
                                    sampleStableRef = face.Reference.ConvertToStableRepresentation(dbDoc);
                                    break;
                                }
                            }
                            else if (geomObj is Curve)
                            {
                                Curve curve = geomObj as Curve;
                                Reference curveRef = curve.Reference;
                                if (curveRef != null)
                                {
                                    sampleStableRef = curve.Reference.ConvertToStableRepresentation(dbDoc);
                                    break;
                                }

                            }
                            else if (geomObj is Point)
                            {
                                Point point = geomObj as Point;
                                sampleStableRef = point.Reference.ConvertToStableRepresentation(dbDoc);
                                break;
                            }
                        }
                    }

                    if (sampleStableRef != null)
                    {
                        String[] refTokens = sampleStableRef.Split(new char[] { ':' });

                        String customStableRef = refTokens[0] + ":"
                          + refTokens[1] + ":" + refTokens[2] + ":"
                          + refTokens[3] + ":" + idx.ToString();

                        indexRef = Reference.ParseFromStableRepresentation(dbDoc, customStableRef);

                        GeometryObject geoObj = inst.GetGeometryObjectFromReference(indexRef);

                        if (geoObj != null)
                        {
                            String finalToken = "";
                            if (geoObj is Edge)
                            {
                                finalToken = ":LINEAR";
                            }

                            if (geoObj is Face)
                            {
                                finalToken = ":SURFACE";
                            }

                            customStableRef += finalToken;
                            indexRef = Reference.ParseFromStableRepresentation(dbDoc, customStableRef);
                        }
                        else
                        {
                            indexRef = null;
                        }
                    }
                }
                else
                {
                    throw new Exception("No Symbol Geometry found...");
                }
            }
            return indexRef;
        }

        private bool IsLineVertical(Line line)
        {
            if (line.Direction.IsAlmostEqualTo(XYZ.BasisZ) || line.Direction.IsAlmostEqualTo(-XYZ.BasisZ))
                return true;
            else
                return false;
        }

        private Face GetFace(Element selectedElem, XYZ orientation)
        {
            // create variable for face
            PlanarFace returnFace = null;

            // get all the wall solids
            List<Solid> solids = GetSolids(selectedElem);

            // loop through each solid
            foreach (Solid curSolid in solids)
            {
                // loop through the faces of the solid
                foreach (Face curFace in curSolid.Faces)
                {
                    // check if planar face
                    if (curFace is PlanarFace)
                    {
                        // cast as planar face
                        PlanarFace curPF = (PlanarFace)curFace;

                        // check if normals are almost equal
                        if (curPF.FaceNormal.IsAlmostEqualTo(orientation))                        
                            returnFace = curPF;
                    }
                }
            }

            return returnFace;
        }

        private List<Solid> GetSolids(Element selectedElem)
        {
            List<Solid> m_returnList = new List<Solid>();

            // create an options variable
            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.DetailLevel = ViewDetailLevel.Fine;

            // get the geometry element
            GeometryElement geomElem = selectedElem.get_Geometry(opt);

            // loop through each geometry object in the element
            foreach (GeometryObject curObject in geomElem)
            {
                // check if solid
                if (curObject is Solid)
                {
                    Solid curSolid = (Solid)curObject;

                    // check if solid volume is greater than 0 & # of faces > 0
                    if (curSolid.Volume > 0.0 && curSolid.Faces.Size > 0)
                    {
                        // add solid to return list
                        m_returnList.Add(curSolid);
                    }
                }
            }

            return m_returnList;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            Common.ButtonDataClass myButtonData = new Common.ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData.Data;
        }
    }

}
