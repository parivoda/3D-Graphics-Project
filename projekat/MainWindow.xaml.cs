using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using WpfApp1.Model;
using Point = WpfApp1.Model.Point;

namespace projekat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region definisanje listi if dictionarya
        private GeometryModel3D hitgeo;
        System.Windows.Point mousePositionForToolTip = new System.Windows.Point();

        public const double minLon = 19.793909;
        public const double minLat = 45.2325;
        public const double maxLon = 19.894459;
        public const double maxLat = 45.277031;

        public List<LineEntity> linesList = new List<LineEntity>();
        public List<NodeEntity> nodeList = new List<NodeEntity>();
        public List<SubstationEntity> substationList = new List<SubstationEntity>();
        public List<SwitchEntity> switchList = new List<SwitchEntity>();

        private static Dictionary<long, PowerEntity> entityCollection = new Dictionary<long, PowerEntity>();

        Dictionary<GeometryModel3D, PowerEntity> entityGeo = new Dictionary<GeometryModel3D, PowerEntity>();
        Dictionary<GeometryModel3D, LineEntity> lineGeo = new Dictionary<GeometryModel3D, LineEntity>();
        //menjanje boja
        Dictionary<GeometryModel3D, long> allEntities = new Dictionary<GeometryModel3D, long>();
        Dictionary<GeometryModel3D, long> allLines = new Dictionary<GeometryModel3D, long>();
        //entiteti na istom mestu
        private static Dictionary<System.Windows.Point, int> numberOfEntityOnPoint = new Dictionary<System.Windows.Point, int>();

        ToolTip toolTip = new ToolTip();
        //dodatni koji ne radi bas?
        public List<GeometryModel3D> lineGeometryModel = new List<GeometryModel3D>();

        int currentZoom = 1;
        int maxZoom = 20;
        int minZoom = -5;
        //za pan
        private System.Windows.Point start = new System.Windows.Point();
        private System.Windows.Point diffOffset = new System.Windows.Point();
        //za rotiranje
        private System.Windows.Point startPosition = new System.Windows.Point();
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            LoadXml();
            DrawNodes();
            DrawSubstations();
            DrawSwitches();
            DrawLines();
        }
        private void LoadXml()
        {
            double longit = 0; // izlaz iz ToLatLon funkcije 
            double latid = 0;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Geographic.xml");
            XmlNodeList nodeList;
            List<SubstationEntity> subE = new List<SubstationEntity>();
            List<NodeEntity> nodeE = new List<NodeEntity>();
            List<SwitchEntity> switchE = new List<SwitchEntity>();
            List<LineEntity> lineE = new List<LineEntity>();

            var filename = "Geographic.xml";
            var currentDirectory = Directory.GetCurrentDirectory();
            var purchaseOrderFilepath = System.IO.Path.Combine(currentDirectory, filename);
            StringBuilder result = new StringBuilder();
            XDocument xdoc = XDocument.Load(filename);


            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
            foreach (XmlNode node in nodeList)
            {

                SubstationEntity sub = new SubstationEntity();
                sub.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                sub.Name = node.SelectSingleNode("Name").InnerText;
                sub.X = double.Parse(node.SelectSingleNode("X").InnerText);
                sub.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                subE.Add(sub); // dodat odmah iz xml 
            }

            for (int i = 0; i < subE.Count(); i++)
            {
                var item = subE[i];
                ToLatLon(item.X, item.Y, 34, out latid, out longit);
                if (latid >= minLat && latid <= maxLat && longit >= minLon && longit <= maxLon)
                {
                    subE[i].Latitude = latid;
                    subE[i].Longitude = longit;
                    substationList.Add(subE[i]);
                    entityCollection.Add(subE[i].Id, subE[i]);
                }

            }

            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
            foreach (XmlNode node in nodeList)
            {

                NodeEntity nodeobj = new NodeEntity();
                nodeobj.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                nodeobj.Name = node.SelectSingleNode("Name").InnerText;
                nodeobj.X = double.Parse(node.SelectSingleNode("X").InnerText);
                nodeobj.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                nodeE.Add(nodeobj);

            }
            for (int i = 0; i < nodeE.Count(); i++)
            {
                var item = nodeE[i];
                ToLatLon(item.X, item.Y, 34, out latid, out longit);
                if (latid >= minLat && latid <= maxLat && longit >= minLon && longit <= maxLon)
                {
                    nodeE[i].Latitude = latid;
                    nodeE[i].Longitude = longit;
                    this.nodeList.Add(nodeE[i]);
                    entityCollection.Add(nodeE[i].Id, nodeE[i]);
                }

            }

            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
            foreach (XmlNode node in nodeList)
            {
                SwitchEntity switchobj = new SwitchEntity();
                switchobj.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                switchobj.Name = node.SelectSingleNode("Name").InnerText;
                switchobj.X = double.Parse(node.SelectSingleNode("X").InnerText);
                switchobj.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                switchobj.Status = node.SelectSingleNode("Status").InnerText;

                switchE.Add(switchobj);
                //switches.Add(switchobj.Id, switchobj);

            }

            for (int i = 0; i < switchE.Count(); i++)
            {
                var item = switchE[i];
                ToLatLon(item.X, item.Y, 34, out latid, out longit);
                if (latid >= minLat && latid <= maxLat && longit >= minLon && longit <= maxLon)
                {
                    switchE[i].Latitude = latid;
                    switchE[i].Longitude = longit;
                    switchList.Add(switchE[i]);
                    entityCollection.Add(switchE[i].Id, switchE[i]);
                }


            }

            var lines = xdoc.Descendants("LineEntity")
                     .Select(line => new LineEntity
                     {
                         Id = (long)line.Element("Id"),
                         Name = (string)line.Element("Name"),
                         ConductorMaterial = (string)line.Element("ConductorMaterial"),
                         IsUnderground = (bool)line.Element("IsUnderground"),
                         R = (float)line.Element("R"),
                         FirstEnd = (long)line.Element("FirstEnd"),
                         SecondEnd = (long)line.Element("SecondEnd"),
                         LineType = (string)line.Element("LineType"),
                         ThermalConstantHeat = (long)line.Element("ThermalConstantHeat"),
                         Vertices = line.Element("Vertices").Descendants("Point").Select(p => new Point
                         {
                             X = (double)p.Element("X"),
                             Y = (double)p.Element("Y"),
                         }).ToList()
                     }).ToList();

            for (int i = 0; i < lines.Count(); i++)
            {
                if (entityCollection.ContainsKey(lines[i].SecondEnd) && entityCollection.ContainsKey(lines[i].FirstEnd))
                {
                    var line = lines[i];
                    foreach (var point in line.Vertices)
                    {

                        ToLatLon(point.X, point.Y, 34, out latid, out longit);
                        point.Latitude = latid;
                        point.Longitude = longit;

                    }
                    linesList.Add(line);
                }
            }
        }
        public void DrawNodes()
        {
            foreach (var node in nodeList)
            {

                MeshGeometry3D meshGeometry3D = CreateCube(node.Longitude, node.Latitude, node.Id);
                DiffuseMaterial diffuseMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.HotPink));

                GeometryModel3D geometryModel3D = new GeometryModel3D(meshGeometry3D, diffuseMaterial);

                RotateTransform3D rotateTransform3D = new RotateTransform3D();

                Transform3DGroup myTransform3DGroup = new Transform3DGroup();
                myTransform3DGroup.Children.Add(rotateTransform3D);

                geometryModel3D.Transform = myTransform3DGroup;

                entityGeo.Add(geometryModel3D, node);
                model3dGroup.Children.Add(geometryModel3D);

                allEntities.Add(geometryModel3D, node.Id);
            }

        }
        public void DrawSubstations()
        {
            foreach (var sub in substationList)
            {
                MeshGeometry3D meshGeometry3D = CreateCube(sub.Longitude, sub.Latitude, sub.Id);
                DiffuseMaterial diffuseMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Blue));

                GeometryModel3D geometryModel3D = new GeometryModel3D(meshGeometry3D, diffuseMaterial);

                RotateTransform3D rotateTransform3D = new RotateTransform3D();

                Transform3DGroup myTransform3DGroup = new Transform3DGroup();
                myTransform3DGroup.Children.Add(rotateTransform3D);

                geometryModel3D.Transform = myTransform3DGroup;

                entityGeo.Add(geometryModel3D, sub);
                model3dGroup.Children.Add(geometryModel3D);

                allEntities.Add(geometryModel3D, sub.Id);
            }
        }
        public void DrawSwitches()
        {
            foreach (var sw in switchList)
            {
                MeshGeometry3D meshGeometry3D = CreateCube(sw.Longitude, sw.Latitude, sw.Id);
                DiffuseMaterial diffuseMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Orange));

                GeometryModel3D geometryModel3D = new GeometryModel3D(meshGeometry3D, diffuseMaterial);

                RotateTransform3D rotateTransform3D = new RotateTransform3D();

                Transform3DGroup myTransform3DGroup = new Transform3DGroup();
                myTransform3DGroup.Children.Add(rotateTransform3D);

                geometryModel3D.Transform = myTransform3DGroup;

                entityGeo.Add(geometryModel3D, sw);
                model3dGroup.Children.Add(geometryModel3D);

                allEntities.Add(geometryModel3D, sw.Id);
            }
        }
        public void DrawLines()
        {
            foreach (var line in linesList)
            {
                System.Windows.Point point1 = new System.Windows.Point();
                System.Windows.Point point2 = new System.Windows.Point();
                var temp = 1;
                System.Windows.Point Startpoint;
                System.Windows.Point Endpoint;
                GeometryModel3D vod;
                DiffuseMaterial boja = new DiffuseMaterial();

                if (entityCollection.ContainsKey(line.FirstEnd) && entityCollection.ContainsKey(line.SecondEnd))
                {
                    Startpoint = CreatePoint(entityCollection[line.FirstEnd].Longitude, entityCollection[line.FirstEnd].Latitude);
                    Endpoint = CreatePoint(entityCollection[line.SecondEnd].Longitude, entityCollection[line.SecondEnd].Latitude);

                }
                else
                {
                    continue;
                }

                if (line.ConductorMaterial == "Steel")
                    boja = new DiffuseMaterial(System.Windows.Media.Brushes.Green);
                else if (line.ConductorMaterial == "Acsr")
                    boja = new DiffuseMaterial(System.Windows.Media.Brushes.Red);
                else if (line.ConductorMaterial == "Copper")
                    boja = new DiffuseMaterial(System.Windows.Media.Brushes.Black);

                foreach (var point in line.Vertices)
                {
                    if (temp == 1)
                    {
                        point1 = CreatePoint(point.Longitude, point.Latitude);
                        model3dGroup.Children.Add(CreateLines(Startpoint, point1, boja));
                        temp++;
                        continue;
                    }
                    else if (temp == 2)
                    {
                        point2 = CreatePoint(point.Longitude, point.Latitude);
                        model3dGroup.Children.Add(CreateLines(point1, point2, boja));
                        point1 = point2;
                    }
                }

                vod = CreateLines(point2, Endpoint, boja);

                allLines.Add(vod, line.Id);
                lineGeo.Add(vod, line);
                model3dGroup.Children.Add(CreateLines(point2, Endpoint, boja));
                lineGeometryModel.Add(vod);
                allEntities.Add(vod, line.Id);
            }
        }
        //zumiranje
        private void ViewPort_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            toolTip.IsOpen = false;

            System.Windows.Point p = e.MouseDevice.GetPosition(this);

            double scaleX;
            double scaleY;
            double scaleZ;

            if (e.Delta > 0 && currentZoom < maxZoom)
            {
                scaleX = skaliranje.ScaleX + 0.1;
                scaleY = skaliranje.ScaleY + 0.1;
                scaleZ = skaliranje.ScaleZ + 0.1;
                currentZoom++;
                skaliranje.ScaleX = scaleX;
                skaliranje.ScaleY = scaleY;
                skaliranje.ScaleZ = scaleZ;
            }
            else if (e.Delta <= 0 && currentZoom > minZoom)
            {
                scaleX = skaliranje.ScaleX - 0.1;
                scaleY = skaliranje.ScaleY - 0.1;
                scaleZ = skaliranje.ScaleZ - 0.1;
                currentZoom--;
                skaliranje.ScaleX = scaleX;
                skaliranje.ScaleY = scaleY;
                skaliranje.ScaleZ = scaleZ;
            }
        }
        //pan
        private void ViewPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            toolTip.IsOpen = false; //UKLONI Tooltip nakon klika

            ViewPort.CaptureMouse();
            start = e.GetPosition(ViewPort);
            diffOffset.X = translacija.OffsetX;
            diffOffset.Y = translacija.OffsetY;
        }
        //rotiranje
        private void ViewPort_MouseMove(object sender, MouseEventArgs e)
        {

            if (ViewPort.IsMouseCaptured)
            {
                System.Windows.Point end = e.GetPosition(this);
                double offsetX = end.X - start.X;
                double offsetY = end.Y - start.Y;
                double w = Width;
                double h = Height;
                double translateX = (offsetX * 100) / w * 100;
                double translateY = -(offsetY * 100) / h * 100;
                translacija.OffsetX = diffOffset.X + (translateX / (100 * skaliranje.ScaleX)) *10;
                translacija.OffsetY = diffOffset.Y + (translateY / (100 * skaliranje.ScaleX)) *10;
                Console.WriteLine($"offsetX = {translacija.OffsetX}, offsetY = {translacija.OffsetY}");
            }

            System.Windows.Point currentPosition = e.GetPosition(this);
            if (e.MiddleButton == MouseButtonState.Pressed)
            {

                double pomerajX = currentPosition.X - startPosition.X;
                double pomerajY = currentPosition.Y - startPosition.Y;
                double step = 0.2;
                if ((rotateX.Angle + step * pomerajY) < 90 && (rotateX.Angle + step * pomerajY) > -90)
                    rotateX.Angle += step * pomerajY;
                if ((rotateY.Angle + step * pomerajX) < 90 && (rotateY.Angle + step * pomerajX) > -90)
                    rotateY.Angle += step * pomerajX;
            }
            startPosition = currentPosition;
        }
        private void ViewPort_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ViewPort.ReleaseMouseCapture();
        }
        public void SwitchMenjanjeBoje(object sender, RoutedEventArgs e)
        {
            if (promenaBojeSwitch.IsChecked == true)
            {
                foreach (SwitchEntity svic in switchList)
                {
                    if (svic.Status.ToLower().Equals("open"))
                    {
                        allEntities.FirstOrDefault(x => x.Value == svic.Id).Key.Material = new DiffuseMaterial(System.Windows.Media.Brushes.Green);
                    }
                    else if (svic.Status.ToLower().Equals("closed"))
                    {
                        allEntities.FirstOrDefault(x => x.Value == svic.Id).Key.Material = new DiffuseMaterial(System.Windows.Media.Brushes.Red);
                    }
                }
            }
            else if (promenaBojeSwitch.IsChecked == false)
            {
                foreach (SwitchEntity svic in switchList)
                {
                    allEntities.FirstOrDefault(x => x.Value == svic.Id).Key.Material = new DiffuseMaterial(System.Windows.Media.Brushes.Orange);
                }
            }
        }

        public void VodoviMenjanjeBoje(object sender, RoutedEventArgs e)
        {

            if (promenaBojeVodova.IsChecked == true)
            {
                foreach (LineEntity line in linesList)
                {
                    foreach (KeyValuePair<GeometryModel3D, long> gm in allLines)
                    {
                        if (gm.Value == line.Id)
                        {
                            if (line.R < 1)
                                ((DiffuseMaterial)gm.Key.Material).Brush = System.Windows.Media.Brushes.Red;
                            else if (line.R >= 1 && line.R <= 2)
                                ((DiffuseMaterial)gm.Key.Material).Brush = System.Windows.Media.Brushes.Orange;
                            else if (line.R > 2)
                                ((DiffuseMaterial)gm.Key.Material).Brush = System.Windows.Media.Brushes.Yellow;
                        }
                    }
                }
            }
            else if (promenaBojeVodova.IsChecked == false)
            {
                foreach (LineEntity line in linesList)
                {
                    foreach (KeyValuePair<GeometryModel3D, long> gm in allLines)
                    {
                        if (gm.Value == line.Id)
                        {
                            if (line.ConductorMaterial == "Steel")
                                ((DiffuseMaterial)gm.Key.Material).Brush = System.Windows.Media.Brushes.DarkGreen;
                            else if (line.ConductorMaterial == "Acsr")
                                ((DiffuseMaterial)gm.Key.Material).Brush = System.Windows.Media.Brushes.DarkRed;
                            else if (line.ConductorMaterial == "Copper")
                                ((DiffuseMaterial)gm.Key.Material).Brush = System.Windows.Media.Brushes.Black;
                        }
                    }
                }
            }

        }
        private void ViewPort_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePositionForToolTip = e.GetPosition(ViewPort);
            Point3D testpoint3D = new Point3D(mousePositionForToolTip.X, mousePositionForToolTip.Y, 0);
            Vector3D testdirection = new Vector3D(mousePositionForToolTip.X, mousePositionForToolTip.Y, 10);

            PointHitTestParameters pointparams = new PointHitTestParameters(mousePositionForToolTip);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

            hitgeo = null;
            VisualTreeHelper.HitTest(ViewPort, null, HTResult, pointparams);
        }

        //dodatni zadatak
        //private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    var checkBox = sender as CheckBox;
        //    if (checkBox.Content.ToString() == "0-1")
        //    {
        //        foreach (var geoModel in lineGeometryModel)
        //        {
        //            foreach (LineEntity line in linesList)
        //            {
        //                foreach (KeyValuePair<GeometryModel3D, long> gm in allLines)
        //                {
        //                    if (gm.Value == line.Id)
        //                    {
        //                        if (line.R < 1)
        //                            model3dGroup.Children.Remove(geoModel);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else if (checkBox.Content.ToString() == "1-2")
        //    {
        //        foreach (var geoModel in lineGeometryModel)
        //        {
        //            foreach (LineEntity line in linesList)
        //            {
        //                foreach (KeyValuePair<GeometryModel3D, long> gm in allLines)
        //                {
        //                    if (gm.Value == line.Id)
        //                    {
        //                        if (line.R >= 1 && line.R <= 2)
        //                            model3dGroup.Children.Remove(geoModel);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else if (checkBox.Content.ToString() == "2-")
        //    {
        //        foreach (var geoModel in lineGeometryModel)
        //        {
        //            foreach (LineEntity line in linesList)
        //            {
        //                foreach (KeyValuePair<GeometryModel3D, long> gm in allLines)
        //                {
        //                    if (gm.Value == line.Id)
        //                    {
        //                        if (line.R > 2)
        //                            model3dGroup.Children.Remove(geoModel);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
        //private void CheckBox_Checked(object sender, RoutedEventArgs e)
        //{
        //    var checkBox = sender as CheckBox;

        //    if (checkBox.Content.ToString() == "0-1")
        //    {
        //        foreach (var geoModel in lineGeometryModel)
        //        {
        //            foreach (LineEntity line in linesList)
        //            {
        //                foreach (KeyValuePair<GeometryModel3D, long> gm in allLines)
        //                {
        //                    if (gm.Value == line.Id)
        //                    {
        //                        if (line.R < 1)
        //                            model3dGroup.Children.Add(geoModel);
        //                    }
        //                }
        //            }
        //        }  
        //    }
        //    else if (checkBox.Content.ToString() == "1-2")
        //    {
        //        foreach (var geoModel in lineGeometryModel)
        //        {
        //            foreach (LineEntity line in linesList)
        //            {
        //                foreach (KeyValuePair<GeometryModel3D, long> gm in allLines)
        //                {
        //                    if (gm.Value == line.Id)
        //                    {
        //                        if (line.R >= 1 && line.R <= 2)
        //                            model3dGroup.Children.Add(geoModel);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else if (checkBox.Content.ToString() == "2-")
        //    {
        //        foreach (var geoModel in lineGeometryModel)
        //        {
        //            foreach (LineEntity line in linesList)
        //            {
        //                foreach (KeyValuePair<GeometryModel3D, long> gm in allLines)
        //                {
        //                    if (gm.Value == line.Id)
        //                    {
        //                        if (line.R > 2)
        //                            model3dGroup.Children.Add(geoModel);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        public static GeometryModel3D CreateLines(System.Windows.Point point1, System.Windows.Point point2, DiffuseMaterial boja)
        {
            MeshGeometry3D meshGeometry3D = new MeshGeometry3D();
            List<Point3D> points = new List<Point3D>();
            points.Add(new Point3D(point1.X - 1, point1.Y - 1, 0)); //0
            points.Add(new Point3D(point1.X + 1, point1.Y + 1, 0)); //1
            points.Add(new Point3D(point1.X - 1, point1.Y - 1, 10)); //2
            points.Add(new Point3D(point1.X + 1, point1.Y + 1, 10)); //3

            points.Add(new Point3D(point2.X - 1, point2.Y - 1, 0)); //4
            points.Add(new Point3D(point2.X + 1, point2.Y + 1, 0)); //5
            points.Add(new Point3D(point2.X - 1, point2.Y - 1, 10)); //6
            points.Add(new Point3D(point2.X + 1, point2.Y + 1, 10)); //7


            meshGeometry3D.Positions = new Point3DCollection(points);
            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(3);
            meshGeometry3D.TriangleIndices.Add(3);
            meshGeometry3D.TriangleIndices.Add(1);
            meshGeometry3D.TriangleIndices.Add(0); // back

            meshGeometry3D.TriangleIndices.Add(4);
            meshGeometry3D.TriangleIndices.Add(6);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(4); // left

            meshGeometry3D.TriangleIndices.Add(5);
            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(6);
            meshGeometry3D.TriangleIndices.Add(6);
            meshGeometry3D.TriangleIndices.Add(4);
            meshGeometry3D.TriangleIndices.Add(5); //front

            meshGeometry3D.TriangleIndices.Add(1);
            meshGeometry3D.TriangleIndices.Add(3);
            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(5);
            meshGeometry3D.TriangleIndices.Add(1); // right

            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(3);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(6);
            meshGeometry3D.TriangleIndices.Add(7); // top

            meshGeometry3D.TriangleIndices.Add(1);
            meshGeometry3D.TriangleIndices.Add(5);
            meshGeometry3D.TriangleIndices.Add(4);
            meshGeometry3D.TriangleIndices.Add(4);
            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(1); // bottom

            GeometryModel3D geometryModel3D = new GeometryModel3D(meshGeometry3D, boja);

            RotateTransform3D rotateTransform3D = new RotateTransform3D();

            Transform3DGroup myTransform3DGroup = new Transform3DGroup();
            myTransform3DGroup.Children.Add(rotateTransform3D);

            geometryModel3D.Transform = myTransform3DGroup;

            return geometryModel3D;
        }
        public static MeshGeometry3D CreateCube(double longitude, double latitude, long entityID)
        {
            var point = CreatePoint(longitude, latitude);
            int numEntity;
            //broji koliko entiteta stoji u jednom mestu
            if (numberOfEntityOnPoint.ContainsKey(point))
            {
                numberOfEntityOnPoint[point]++;
                numEntity = numberOfEntityOnPoint[point];
            }
            else
            {
                numberOfEntityOnPoint[point] = 1;
                numEntity = 1;
            }

            MeshGeometry3D meshGeometry3D = new MeshGeometry3D();

            List<Point3D> uglovi = new List<Point3D>();
            //8 tacaka
            uglovi.Add(new Point3D(point.X - 4, point.Y - 4, numEntity * 10 - 10));//0
            uglovi.Add(new Point3D(point.X + 4, point.Y - 4, numEntity * 10 - 10));//1
            uglovi.Add(new Point3D(point.X + 4, point.Y - 4, numEntity * 10));
            uglovi.Add(new Point3D(point.X - 4, point.Y - 4, numEntity * 10));
            uglovi.Add(new Point3D(point.X - 4, point.Y + 4, numEntity * 10));
            uglovi.Add(new Point3D(point.X + 4, point.Y + 4, numEntity * 10));
            uglovi.Add(new Point3D(point.X + 4, point.Y + 4, numEntity * 10 - 10));//6
            uglovi.Add(new Point3D(point.X - 4, point.Y + 4, numEntity * 10 - 10));//7
            //0,1,6,7 donje tacke

            meshGeometry3D.Positions = new Point3DCollection(uglovi);
            // stranice
            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(1);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(3);

            meshGeometry3D.TriangleIndices.Add(3);
            meshGeometry3D.TriangleIndices.Add(5);
            meshGeometry3D.TriangleIndices.Add(4);
            meshGeometry3D.TriangleIndices.Add(3);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(5);

            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(6);
            meshGeometry3D.TriangleIndices.Add(5);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(1);
            meshGeometry3D.TriangleIndices.Add(6);

            meshGeometry3D.TriangleIndices.Add(3);
            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(4);
            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(3);

            meshGeometry3D.TriangleIndices.Add(4);
            meshGeometry3D.TriangleIndices.Add(6);
            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(4);
            meshGeometry3D.TriangleIndices.Add(5);
            meshGeometry3D.TriangleIndices.Add(6);

            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(1);
            meshGeometry3D.TriangleIndices.Add(1);
            meshGeometry3D.TriangleIndices.Add(7);
            meshGeometry3D.TriangleIndices.Add(6);

            return meshGeometry3D;
        }
        private HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {
            RayHitTestResult rayResult = rawresult as RayHitTestResult;

            if (rayResult != null)
            {
                bool gasit = false;
                for (int i = 0; i < entityGeo.Keys.Count(); i++)
                {
                    if (entityGeo.Keys.ToList()[i] == rayResult.ModelHit)
                    {
                        hitgeo = (GeometryModel3D)rayResult.ModelHit;
                        gasit = true;
                        var entity = entityGeo[hitgeo];

                        toolTip = new ToolTip();
                        toolTip.Content = "\tPOWER ENTITY:\nID: " + entity.Id.ToString() + "\nName: " + entity.Name + "\nType: " + entity.GetType().Name;
                        toolTip.Height = 80;
                        toolTip.Background = Brushes.DarkSlateGray;
                        toolTip.Foreground = Brushes.AliceBlue;
                        toolTip.IsOpen = true;
                        ToolTipService.SetPlacement(ViewPort, System.Windows.Controls.Primitives.PlacementMode.Mouse);
                        break;
                    }
                    else
                    {
                        toolTip.IsOpen = false;
                    }
                }
                //for (int i = 0; i < lineGeo.Keys.Count(); i++)
                //{
                //    if (lineGeo.Keys.ToList()[i] == rayResult.ModelHit)
                //    {
                //        hitgeo = (GeometryModel3D)rayResult.ModelHit;
                //        gasit = true;
                //        var line = lineGeo[hitgeo];

                //        toolTip = new ToolTip();
                //        toolTip.Content = "\tLINE:\nID: " + line.Id.ToString() + "\nName: " + line.Name + "\nStartNode: " + line.FirstEnd + "\nEndNode: " + line.SecondEnd;
                //        toolTip.Height = 80;
                //        toolTip.Background = Brushes.DarkSlateGray;
                //        toolTip.Foreground = Brushes.AliceBlue;
                //        toolTip.IsOpen = true;
                //        ToolTipService.SetPlacement(ViewPort, System.Windows.Controls.Primitives.PlacementMode.Mouse);
                //        break;
                //    }
                //    else
                //    {
                //        toolTip.IsOpen = false;
                //    }
                //}
                if (!gasit)
                {
                    hitgeo = null;
                }
            }
            return HitTestResultBehavior.Stop;
        }
        public static System.Windows.Point CreatePoint(double longitude, double latitude)
        {
            
            double vrednostJednogLongitude = (maxLon - minLon) / 1175; // width slike mape
            double vrednostJednogLatitude = (maxLat - minLat) / 775; // height slike mape 

            // koliko stane u rastojanje izmedju ttrenutne i minimalne 
            double x = Math.Round((longitude - minLon) / vrednostJednogLongitude) - 587.5; // pozicija na xamlu
            double y = Math.Round((latitude - minLat) / vrednostJednogLatitude) - 387.5;

            // zaokruzi na prvi broj deljiv sa 10 
            x = x - x % 8; // rastojanje izmedju dva susedna x
            y = y - y % 8; // rastojanje izmedju dva susedna y 

            System.Windows.Point point = new System.Windows.Point();
            point.X = x;
            point.Y = y;

            return point;
        }
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }
    }
}
