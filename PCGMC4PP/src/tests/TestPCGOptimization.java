/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package tests;

import game.GameState;
import lgraphs.LGraph;
import support.GameStateExporter;
import static support.PCG.embeddGraph;
import static support.PCG.generateGraph;
import static tests.GrammarTest_.generateGraph;

/**
 *
 * @author josepvalls
 */
public class TestPCGOptimization {

    public static void main(String args[]) throws Exception {
         LGraph graph = generateGraph(6, 5, true, false);
         for(int i=0;i<1;i++){
             System.out.println("USING SEED "+Integer.toString(i));
             GameState gs = embeddGraph(graph, i, true);
             //System.out.println(gs.toString());
             System.out.println(GameStateExporter.export(gs));
         }        
     }

}
