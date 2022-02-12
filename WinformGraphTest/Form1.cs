using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using static Microsoft.Msagl.Core.Layout.Label;
using GColor = Microsoft.Msagl.Drawing.Color;

namespace WinformGraphTest {
    public partial class Form1 : Form {
        /* Links:
         *  - https://www.codeguru.com/dotnet/visualizing-nodes-and-edges-with-microsoft-automatic-graph-layout/
         *  - https://github.com/microsoft/automatic-graph-layout
         */
        
        public Form1() {
            InitializeComponent();
            GViewer viewer = new GViewer();
            
            //viewer.Graph = GenerateTestGraph1();
            viewer.Graph = GenerateTestGraph2();

            SuspendLayout();
            viewer.Dock = DockStyle.Fill;
            Controls.Add(viewer);
            ResumeLayout();
        }

        private Graph GenerateTestGraph1() {
            
            Graph graph = new Graph();
            graph.AddEdge("CHILD1", "Room", "KIDSROOM");
            graph.AddEdge("CHILD2", "Room", "KIDSROOM");
            graph.AddEdge("CHILD3", "Room", "KIDSROOM");
            graph.AddEdge("CHILD1", "Type", "TYPE");
            graph.AddEdge("CHILD2", "Type", "TYPE");
            graph.AddEdge("CHILD3", "Type", "TYPE");
            graph.AddEdge("CHILD1", "Parent", "PARENT");
            graph.AddEdge("CHILD2", "Parent", "PARENT");
            graph.AddEdge("CHILD3", "Parent", "PARENT");
            graph.AddEdge("PARENT", "Parent", "GRANDPARENT");
            return graph;
        }

        private Graph GenerateTestGraph2() {
            
            Dictionary<string, JArray> data = new Dictionary<string, JArray>();
            data.Add("CHILD", new JArray() {
                new JObject() {
                    new JProperty("Name", "CHILD1"),
                    new JProperty("Id", 100),
                    new JProperty("Type", new JObject() {new JProperty("Id", 2000) }),
                    new JProperty("Room", new JObject() {new JProperty("Id", 1000) }),
                    new JProperty("Parent", new JObject() { new JProperty("Id", 10000) })
                },
                new JObject() {
                    new JProperty("Name", "CHILD2"),
                    new JProperty("Id", 200),
                    new JProperty("Type", new JObject() {new JProperty("Id", 2000) }),
                    new JProperty("Room", new JObject() {new JProperty("Id", 1000) }),
                    new JProperty("Parent", new JObject() { new JProperty("Id", 10000) })
                },
                new JObject() {
                    new JProperty("Name", "CHILD3"),
                    new JProperty("Id", 300),
                    new JProperty("Type", new JObject() {new JProperty("Id", 2000) }),
                    new JProperty("Room", new JObject() {new JProperty("Id", 1000) }),
                    new JProperty("Parent", new JObject() { new JProperty("Id", 10000) })
                }}
            );
            data.Add("Room", new JArray() {
                new JObject() {
                    new JProperty("Name", "KidsdRoom"),
                    new JProperty("Id", 1000)
                }
            });
            
            data.Add("Type", new JArray() {
                new JObject() {
                    new JProperty("Name", "SomeTYpe"),
                    new JProperty("Id", 2000)
                } 
            });
            data.Add("Parent", new JArray() {
                new JObject() {
                    new JProperty("Name", "Parent"),
                    new JProperty("Id", 10000),
                    new JProperty("GrandParent", new JObject() { new JProperty("Id", 10100) }),
                } 
            });
            data.Add("GrandParent", new JArray() {
                new JObject() {
                    new JProperty("Name", "GrandParent"),
                    new JProperty("Id", 10100),

                } 
            });
            Graph g = CreateGraph(data);

            return g;
        }

        

        private Graph CreateGraph(Dictionary<string, JArray> data) {
            Graph graph = new Graph("graph");
            
            Dictionary<int, JObject> dos = new Dictionary<int, JObject>();
            foreach (string dataObjectType in data.Keys) {
                JArray dataObjects = data[dataObjectType];

                foreach (JObject dataObject in dataObjects) { 
                    dos.Add(ID(dataObject), dataObject);
                }
            }

            int colorIndex = 0;
            List<GColor> colors = GenerateColors();
            foreach (string dataObjectType in data.Keys) {

                GColor color = colors[colorIndex];
                colorIndex++;
                JArray dataObjects = data[dataObjectType];

                foreach (JObject dataObject in dataObjects) { 
                
                    string nodeData = CreateNodeData(dataObject);
                    graph.AddNode(nodeData);
                    DecorateNode(graph, nodeData, color);

                    foreach (KeyValuePair<string, JToken> property in dataObject) {

                        if (property.Value.GetType() == typeof(JObject)) {

                            if (!dos.ContainsKey(property.Value["Id"].ToObject<int>())) continue;
                            CreateEdge(graph, dataObject, dos[ID((JObject)property.Value)], property.Key, color);

                        } else if (property.Value.GetType() == typeof(JArray)) { 
                            JArray values = (JArray)property.Value;
                            foreach (JObject value in values) {
                                if (!dos.ContainsKey(value["Id"].ToObject<int>())) continue;
                                CreateEdge(graph, dataObject, dos[ID(value)], property.Key, color);
                            }
                        }
                    }
                }
            }
            
            foreach (string dataObjectType in data.Keys) {
                JArray dataObjects = data[dataObjectType];

                foreach (JObject dataObject in dataObjects) { 
                    string nodeData = CreateNodeData(dataObject);
                    Node node = graph.FindNode(nodeData);
                    node.LabelText = $"{dataObjectType}\n{node.LabelText}";
                }
            }
            
            return graph;
        }

        private List<GColor> GenerateColors() {
            List<GColor> colors = new List<GColor>();
            int min = 200;
            int max = 255;
            int step = 50;
            for(int r = min; r <= max; r += step ) {
                for (int g = min; g <= max; g += step) {
                    for (int b = min; b <= max; b += step) {
                        colors.Add(new GColor((byte)r, (byte)g, (byte)b));
                    }
                }
            }
            return colors;
        }

        private static int ID(JObject jObject) => jObject["Id"].ToObject<int>();
        private static string CreateNodeData(JObject jObj) => $"{jObj["DisplayName"]}\n{jObj["Id"]}";

        private static void CreateEdge(Graph graph, JObject start, JObject end, string property, GColor color) {
            
            string node1 = CreateNodeData(start);
            string node2 = CreateNodeData(end);
            graph.AddEdge(node1, property, node2).Attr.Color = GColor.LightBlue;
            //DecorateNode(graph, node1, color);
            //DecorateNode(graph, node2, color);
        }

        private static void DecorateNode(Graph graph, string node, GColor color) {
            int padding = 10;
            graph.FindNode(node).Attr.FillColor = color;
            graph.FindNode(node).Attr.LabelMargin = padding;
        }
    }
}
