using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using QuikGraph;
using System.Diagnostics;

namespace Dream_Charter {
    internal static partial class Interfacing {

        private const string SAVE_SVG_NAME = "visualization.svg";
        private static readonly string SAVE_SVG_PATH = Path.Combine(DreamLocation.DATA_FOLDER, SAVE_SVG_NAME);

        private const int VISUALIZATION_WIDTH = 1000;
        private const int VISUALIZATION_HEIGHT = 1000;

        private static Graph _toMsagl(AdjacencyGraph<string, TaggedEdge<string, int>> quick_graph) {
            // msagl graph
            Graph msagl_graph = new();

            // get nodes
            foreach (string node in quick_graph.Vertices) {
                msagl_graph.AddNode(new Node(node));
            }

            // get edges
            foreach (Edge<string> edge in quick_graph.Edges) {
                msagl_graph.AddEdge(edge.Source, edge.Target);
            }

            // return graph
            return msagl_graph;
        }

        private static void _showVisualiation(Graph msagl_graph) {
            var geometryGraph = msagl_graph.CreateGeometryGraph();

            // create a set up renderer
            GraphRenderer renderer = new(msagl_graph);
            renderer.CalculateLayout();

            // save file
            using var file = File.Create(SAVE_SVG_PATH);
            var writer = new SvgGraphWriter(file, msagl_graph);
            writer.Write();

            // open users preferred svg viewer
            Process.Start(new ProcessStartInfo {
                FileName = SAVE_SVG_PATH,
                UseShellExecute = true
            });

            Console.WriteLine("showing graph...");
        }

        private static void _show() {
            // convert to msagl save and show
            Graph msagl_graph = _toMsagl(DreamLocation.GRAPH);
            _showVisualiation(msagl_graph);
        }
    }
}
