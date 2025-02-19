using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitAPI_Project1
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSlabCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument UiDoc = commandData.Application.ActiveUIDocument;
            Document Doc = UiDoc.Document;

            // Get Level
            Level level = new FilteredElementCollector(Doc)
                            .OfClass(typeof(Level))
                            .Cast<Level>()
                            .FirstOrDefault(l => l.Name.Equals("Level 2"));

            if (level == null)
            {
                TaskDialog.Show("Error", "Level 'Level 2' not found. Please check your levels.");
                return Result.Failed;
            }

            // Get Slab Type
            FloorType slabType = new FilteredElementCollector(Doc)
                                    .OfClass(typeof(FloorType))
                                    .Cast<FloorType>()
                                    .FirstOrDefault(f => f.Name.Contains("Generic 300mm"));

            if (slabType == null)
            {
                TaskDialog.Show("Error", "Slab type 'Generic 300mm' not found. Please load the correct family.");
                return Result.Failed;
            }

            XYZ originalStart = new XYZ(0, 0, 0);

            // Validate Grid Data
            if (CreateGridCommand.NumberOfHorizontalGrid <= 1 || CreateGridCommand.NumberOfVerticalGrid <= 1)
            {
                TaskDialog.Show("Error", "Invalid grid count. Ensure at least two grids exist in both directions.");
                return Result.Failed;
            }

            if (CreateGridCommand.DistanceBetweenVerticalGrid <= 0 || CreateGridCommand.DistanceBetweenHorizontalGrid <= 0)
            {
                TaskDialog.Show("Error", "Invalid grid spacing. Ensure distances between grids are correctly set.");
                return Result.Failed;
            }

            try
            {
                using (Transaction transaction = new Transaction(Doc, "Create Slab"))
                {
                    transaction.Start();

                    // Calculate slab boundaries
                    double horizontalLength = (CreateGridCommand.NumberOfHorizontalGrid - 1) * CreateGridCommand.DistanceBetweenVerticalGrid;
                    double verticalLength = (CreateGridCommand.NumberOfVerticalGrid - 1) * CreateGridCommand.DistanceBetweenHorizontalGrid;
                    double slabHeight = 300; // Slab height offset

                    // Define boundary lines for the slab
                    Line line1 = Line.CreateBound(originalStart + new XYZ(0, 0, slabHeight), originalStart + new XYZ(horizontalLength, 0, slabHeight));
                    Line line2 = Line.CreateBound(originalStart + new XYZ(horizontalLength, 0, slabHeight), originalStart + new XYZ(horizontalLength, verticalLength, slabHeight));
                    Line line3 = Line.CreateBound(originalStart + new XYZ(horizontalLength, verticalLength, slabHeight), originalStart + new XYZ(0, verticalLength, slabHeight));
                    Line line4 = Line.CreateBound(originalStart + new XYZ(0, verticalLength, slabHeight), originalStart + new XYZ(0, 0, slabHeight));

                    // Create curve loop
                    CurveLoop curveLoop = CurveLoop.Create(new List<Curve>() { line1, line2, line3, line4 });

                    // Create Slab
                    Floor.Create(Doc, new List<CurveLoop>() { curveLoop }, slabType.Id, level.Id);

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
