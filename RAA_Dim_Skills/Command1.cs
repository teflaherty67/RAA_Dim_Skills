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

            }
            else
            {
                // if not a wall, display message and exit
                TaskDialog.Show("Error", "The selected element is not a wall.");
                return Result.Failed;
            }

                return Result.Succeeded;
        }

        private Face GetFace(Element selectedElem, XYZ orientation)
        {
            // create variable for face
            PlanarFace returnFace = null;

            // get all the wall solids
            var solids = GetSolids(selectedElem);

        }

        private object GetSolids(Element selectedElem)
        {
            List<Solid> m_returnList = new List<Solid>();

            // create an options variable
            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.DetailLevel = ViewDetailLevel.Fine;

            // get the geometry element
            GeometryElement geomElem = selectedElem.get_Geometry(opt);



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
