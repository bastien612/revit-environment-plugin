﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Lab1PlaceGroup
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    public class Class1: IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get application and documnet objects
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            try
            {
                //Define a reference Object to accept the pick result
                Reference pickedref = null;

                //Pick a group
                Selection sel = uiapp.ActiveUIDocument.Selection;
                GroupPickFilter selFilter = new GroupPickFilter();
                pickedref = sel.PickObject(ObjectType.Element, selFilter, "Please select a group");

                Element elem = doc.GetElement(pickedref);

                Group group = elem as Group;

                // Get the group center point
                XYZ origin = GetElementCenter(group);

                // Get the room that the picked group is located in
                Room room = GetRoomOfGroup(doc, origin);

                XYZ sourceCenter = GetRoomCenter(room);

                string coords =
                    "X = " + sourceCenter.X + "\r\n" +
                    "Y = " + sourceCenter.Y + "\r\n" +
                    "Z = " + sourceCenter.Z + "\r\n";

                TaskDialog.Show("Source room Center", coords);

                //Pick point
                //XYZ point = sel.PickPoint("Please pick a point to place group");

                //Place the group
                Transaction trans = new Transaction(doc);
                trans.Start("Lab");

                //doc.Create.PlaceGroup(point, group.GroupType);

                // Calculate the new Group position
                XYZ groupLocation = sourceCenter + new XYZ(20, 0, 0);
                doc.Create.PlaceGroup(groupLocation, group.GroupType);

                trans.Commit();
            } catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        // Return the center of an element based on its BoundingBox
        public XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);

            XYZ center = (bounding.Max + bounding.Min) * 0.5;

            return center;
        }

        /// Return the room in which the given point is located
        Room GetRoomOfGroup(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            Room room = null;

            foreach (Element elem in collector)
            {
                room = elem as Room;

                if(room != null)
                {
                    //Decide if this point is in the picked room
                    if(room.IsPointInRoom(point))
                    {
                        break;
                    }
                }
            }
            return room;
        }

        ///Return a room's centzer point coordinates.
        ///Z value is equal to the bottom of the room
        public XYZ GetRoomCenter(Room room)
        {
            //Get the room center point
            XYZ boundCenter = GetElementCenter(room);

            LocationPoint locPt = (LocationPoint)room.Location;
            XYZ roomCenter = new XYZ(boundCenter.X, boundCenter.Y, locPt.Point.Z);


            return roomCenter;
        }
    }

    /// Filter to constrain picking to model groups. Only model groups
    /// are hightlighted and can be selected when cursor is hovering
    
    public class GroupPickFilter:ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            return (e.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_IOSModelGroups));
        }

        public bool AllowReference(Reference r, XYZ p)
        {
            return false;
        }
    } 
}