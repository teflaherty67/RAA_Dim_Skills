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

            }
            else
            {
                // if not a wall, display message and exit
                TaskDialog.Show("Error", "The selected element is not a wall.");
                return Result.Failed;
            }

                return Result.Succeeded;
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
