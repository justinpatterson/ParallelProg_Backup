/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package game.pcg;

import game.GameState;
import java.util.HashMap;
import java.util.Map;
import lgraphs.LGraph;
import lgraphs.LGraphNode;
import orthographicembedding.OrthographicEmbeddingResult;
import support.GameStateExporter;
import tests.GrammarStats;

/**
 *
 * @author josepvalls
 */
public class PuzzleEmbeddingEvaluator {
    LGraph graph;
    LGraph layoutGraph;
    Map<LGraphNode, LGraphNode> map;
    public PuzzleEmbeddingEvaluator(LGraph graph, LGraph layoutGraph, Map<LGraphNode, LGraphNode> map){
        this.graph = graph;
        this.layoutGraph = graph;
        this.map = map;
    }
    double evaluate(GameState gs){
        Map<String, Double> export_map = new HashMap();
        GrammarStats.getVisualProperties(gs, export_map);
        double[] vector = new double[3];
        vector[0] = export_map.get("v_w") / export_map.get("v_h");
        vector[1] = export_map.get("v_trackDensity");
        //vector[2] = export_map.get("v_componentDensity");
        return vector[0]*vector[1];//*vector[2];
    }
    double evaluate(OrthographicEmbeddingResult oe){
        boolean debug = false;
        //return this.evaluate(new GraphManager(oe, this.graph, this.layoutGraph, this.map).graphToAlmostGameState());
        GameState gs = new GraphManager(oe, this.graph, this.layoutGraph, this.map).graphToAlmostGameState();
        double eval = this.evaluate(gs);
        if(debug){
            System.out.println("OGE "+oe);
            String out = GameStateExporter.export(gs);
            System.out.println("EVALUATION "+out);
            System.out.println("EVALUATION "+eval);
        }
        
        return eval;
    }
}
