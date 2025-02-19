using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Linq;

namespace RevitAPI_Project1
{
    [TransactionAttribute(TransactionMode.Manual)]
    internal class CreateColumnsInIntersectionsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument UiDoc = commandData.Application.ActiveUIDocument;
            Document Doc = UiDoc.Document;

            Level level = new FilteredElementCollector(Doc)
                                .OfClass(typeof(Level))
                                .Cast<Level>()
                                .FirstOrDefault(l => l.Name.Equals("Level 1"));

            if (level == null)
            {
                TaskDialog.Show("Error", "Level 'Level 1' not found. Please check your levels.");
                return Result.Failed;
            }

            FamilySymbol columnType = new FilteredElementCollector(Doc)
                                        .OfClass(typeof(FamilySymbol))
                                        .Cast<FamilySymbol>()
                                        .FirstOrDefault(f => f.Name.Contains("450 x 600mm"));

            if (columnType == null)
            {
                TaskDialog.Show("Error", "Column type '450 x 600mm' not found. Please load the correct family.");
                return Result.Failed;
            }

            XYZ originalStart = new XYZ(0, 0, 0);

            if (CreateGridCommand.horizontalGrids == null || CreateGridCommand.verticalGrids == null)
            {
                TaskDialog.Show("Error", "Grid data is missing. Please generate the grids first.");
                return Result.Failed;
            }

            if (CreateGridCommand.horizontalGrids.Count == 0 || CreateGridCommand.verticalGrids.Count == 0)
            {
                TaskDialog.Show("Error", "No grids found. Please create horizontal and vertical grids before running the command.");
                return Result.Failed;
            }

            if (CreateGridCommand.DistanceBetweenVerticalGrid <= 0 || CreateGridCommand.DistanceBetweenHorizontalGrid <= 0)
            {
                TaskDialog.Show("Error", "Invalid grid spacing. Ensure distances between grids are correctly set.");
                return Result.Failed;
            }

            try
            {
                using (Transaction transaction = new Transaction(Doc, "Add Columns"))
                {
                    transaction.Start();

                    if (!columnType.IsActive)
                        columnType.Activate();

                    for (int i = 0; i < CreateGridCommand.horizontalGrids.Count; i++)
                    {
                        for (int j = 0; j < CreateGridCommand.verticalGrids.Count; j++)
                        {
                            XYZ intersectionPoint = originalStart + new XYZ(
                                j * CreateGridCommand.DistanceBetweenVerticalGrid,
                                i * CreateGridCommand.DistanceBetweenHorizontalGrid,
                                0);

                            Doc.Create.NewFamilyInstance(intersectionPoint, columnType, level, StructuralType.Column);
                        }
                    }

                    transaction.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", $"An unexpected error occurred: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
