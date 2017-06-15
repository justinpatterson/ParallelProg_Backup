/*
 * The MIT License
 *
 * Copyright 2016 Josep Valls-Vargas <josep@valls.name>.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
package support;

import orthographicembedding.DisconnectedGraphs;
import game.GameState;
import game.pcg.GraphManager;
import game.pcg.PuzzleEmbeddingComparator;
import game.pcg.PuzzleEmbeddingEvaluator;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Random;
import lgraphs.LGraph;
import lgraphs.LGraphNode;
import lgraphs.ontology.Ontology;
import lgraphs.ontology.Sort;
import lgraphs.sampler.LGraphGrammarSampler;
import lgraphs.sampler.LGraphRewritingGrammar;
import lgraphs.sampler.LGraphRewritingRule;
import optimization.EmbeddingComparator;
import optimization.OrthographicEmbeddingOptimizer;
import optimization.SegmentLengthEmbeddingComparator;
import orthographicembedding.OrthographicEmbedding;
import orthographicembedding.OrthographicEmbeddingResult;
import util.Sampler;
import valls.util.ListToArrayUtility;

/**
 *
 * @author Josep Valls-Vargas <josep@valls.name>
 */
public class PCG {

    public static boolean simplify = true;
    public static boolean correct = true;
    public static void main(String args[]) throws FileNotFoundException, IOException, Exception {
        if (args.length < 2) {
            System.out.println("Usage: support.PCG parameter_file|debug random_seed [keep_solution]");
            System.exit(4);
        }
        boolean keep_solution = false;
        if (args.length >= 3) keep_solution = true;

        String filename = args[0];
        // TODO Get parameters from filename and sample GG with parameters from filename
        long randomSeed = Long.parseLong(args[1]);
        if("debug".equals(filename)) randomSeed = 0;
        GameState gs = generateGameState(randomSeed, keep_solution, ("debug".equals(filename)));
        if(!("debug".equals(filename))){
            // Export
            export(gs, filename, false);
        } else {
            System.out.println(GameStateExporter.export(gs));
        }
        System.exit(0);
    }
    
    public static GameState generateGameState(long randomSeed, boolean keep_solution, boolean debug) throws Exception {
        LGraph graph = generateGraph(randomSeed, 5, keep_solution, false);
        return embeddGraph(graph, randomSeed, debug);
    }

    public static LGraph applyGrammar(Ontology ontology, LGraph graph, String filename, long randomSeed, boolean debug) throws Exception {
        return applyGrammar(ontology, graph, filename, randomSeed, debug, null, null);
    }
    public static LGraph applyGrammar(Ontology ontology, LGraph graph, String filename, long randomSeed, boolean debug, Map<String,Integer> rule_applications, Map<String,Integer> size_application_limits) throws Exception {
        if (debug) {
            System.out.println("Applying " + filename);
        }
        LGraphRewritingGrammar grammar = LGraphRewritingGrammar.load(filename);
        if(rule_applications!=null){
            for(LGraphRewritingRule entry:grammar.getRules()){
                if(!rule_applications.containsKey(entry.getName())){
                    rule_applications.put(entry.getName(), 0);
                }
            }
        }
        
        LGraph lastGraph = graph;
        LGraphGrammarSampler generator = new LGraphGrammarSampler(graph, grammar, true, new Random(randomSeed));
        if(size_application_limits!=null){
            for(Entry<String,Integer> entry:size_application_limits.entrySet()){
                generator.addApplicationLimit(entry.getKey(), entry.getValue());
            }
        }
        // Use the grammar to rewrite the graph:
        do {
            if (debug) {
                System.out.println("Current graph:");
                System.out.println("  " + graph);
            }
            graph = generator.applyRuleStochastically();
            if (graph != null) {
                lastGraph = graph;
            }
        } while (graph != null);
        if (debug) {
            generator.printRuleApplicationCounts();
        }
        if(rule_applications!=null){
            for(Entry<String,Integer> entry:generator.getRuleApplicationCounts().entrySet()){
                rule_applications.put(entry.getKey(), rule_applications.get(entry.getKey())+entry.getValue());
            }
        }
        return lastGraph;
    }

    public static LGraph generateGraph(long randomSeed, int size, boolean keep_solution, boolean debug) throws Exception {
        return generateGraph(randomSeed, size, keep_solution, debug, null);
    }
    public static LGraph generateGraph(long randomSeed, int size, boolean keep_solution, boolean debug, Map<String,Integer> rule_applications) throws Exception {
        List<Integer> sizes = new Sampler(randomSeed).createDistribution(size, 3);
        Map<String,Integer> size_application_limits = new HashMap();
        size_application_limits.put("ADD_MORE_PROBLEMS", sizes.get(0));
        size_application_limits.put("MAKE_SUBPROBLEM_ABST_SERIAL_TASKS", sizes.get(1));
        size_application_limits.put("MAKE_SUBPROBLEM_ABST_PARALLEL_TASKS", sizes.get(2));
        /*
        size_application_limits.put("ADD_MORE_PROBLEMS", 0);
        size_application_limits.put("MAKE_SUBPROBLEM_ABST_SERIAL_TASKS", 1);
        size_application_limits.put("MAKE_SUBPROBLEM_ABST_PARALLEL_TASKS", 1);
        */
        /*
        size_application_limits.put("ADD_MORE_PROBLEMS", 0);
        size_application_limits.put("MAKE_SUBPROBLEM_ABST_SERIAL_TASKS", 0);
        size_application_limits.put("MAKE_SUBPROBLEM_ABST_PARALLEL_TASKS", 0);
        */

        
        Sort.clearSorts();
        Ontology ontology = new Ontology("data/ppppOntology4.xml");
        LGraph graph = LGraph.fromString("N0:problem()");
        // Create structure for problems and subproblems        
        graph = applyGrammar(ontology, graph, "data/ppppGrammar4a.txt", randomSeed, debug, rule_applications, size_application_limits);
        // Instanciate situations
        graph = applyGrammar(ontology, graph, "data/ppppGrammar4b.txt", randomSeed, debug, rule_applications, null);
        // Refine components
        graph = applyGrammar(ontology, graph, "data/ppppGrammar4c.txt", randomSeed, debug, rule_applications, null);
        // Remove solution
        if(!keep_solution){
            graph = applyGrammar(ontology, graph, "data/ppppGrammar4d.txt", randomSeed, debug, rule_applications, null);
        }
        return graph;
    }

    public static GameState embeddGraph(LGraph graph, long randomSeed, boolean debug) throws Exception {
        // calculate the embedding:
        List<Sort> noi = new LinkedList<Sort>();
        List<Sort> eoi = new LinkedList<Sort>();
        noi.add(Sort.getSort("track"));
        eoi.add(Sort.getSort("to"));
        Map<LGraphNode, LGraphNode> map = new HashMap<LGraphNode, LGraphNode>();
        LGraph layoutGraph = graph.cloneSubGraph(noi,eoi, map);
        HashMap<LGraphNode, LGraphNode> map_inverse = ListToArrayUtility.swapMapKeysValues(map);
        
        Random r;
        if(randomSeed>-1){
            r = new Random(randomSeed);
        } else {
            r = new Random();
        }
        
        int[][] aa = layoutGraph.undirectedAdjacencyMatrix();
        if (debug) {
            for (int[] row : aa) {
                for (int col : row) {
                    System.out.print(col);
                    System.out.print(",");
                }
                System.out.println();
            }
        }


        List<List<Integer>> disconnectedGraphs = DisconnectedGraphs.findDisconnectedGraphs(aa);
        List<OrthographicEmbeddingResult> disconnectedEmbeddings = new ArrayList();
        for(List<Integer> nodeSubset:disconnectedGraphs) {
            // calculate the embedding:
            int [][]g = DisconnectedGraphs.subgraph(aa, nodeSubset);
            OrthographicEmbeddingResult g_oe = OrthographicEmbedding.orthographicEmbedding(g,simplify, correct, r); 
            if (g_oe==null) {
                System.err.println("No orthographic projection could be found! verify the input graph is planar.");
                System.exit(10);
            }
            if (!g_oe.sanityCheck(false)) {
                System.err.println("The orthographic projection contains errors!");
                System.exit(11);
            }
            boolean do_custom_optimization = false;
            if(do_custom_optimization){
                //g_oe = OrthographicEmbeddingOptimizer.optimize(g_oe, g, new SegmentLengthEmbeddingComparator());
                g_oe = OrthographicEmbeddingOptimizer.optimize(g_oe, g, new PuzzleEmbeddingComparator(new PuzzleEmbeddingEvaluator(graph, layoutGraph, map_inverse)));
                if (!g_oe.sanityCheck(false)) {
                    System.err.println("The orthographic projection after optimization using custom comparator contains errors!");
                    System.exit(12);
                }
            }
            
            disconnectedEmbeddings.add(g_oe);
        }
        OrthographicEmbeddingResult oe = DisconnectedGraphs.mergeDisconnectedEmbeddingsSideBySide(disconnectedEmbeddings, disconnectedGraphs, 1.0);        
        
        return new GraphManager(oe, graph, layoutGraph, map_inverse).graphToGameState();
    }

    public static void export(GameState gs, String filename, boolean overwrite) throws IOException {
        String out = GameStateExporter.export(gs);
        File file = new File(filename);
        String path = file.getAbsolutePath();
        File out_file;
        if (overwrite) {
            out_file = new File(file.getParentFile(), "pcg_out.txt");
        } else {
            out_file = File.createTempFile("pcg_out_", ".txt", file.getParentFile());
        }
        PrintWriter writer = new PrintWriter(out_file);
        writer.print(out);
        writer.close();
        System.out.println(out_file.getAbsolutePath());
    }
}
