/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package tests;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import game.BoardState;
import game.GameState;
import game.GameStateSearch;
import game.Tile;
import game.component.Component;
import game.component.ComponentDelivery;
import game.component.ComponentDiverter;
import game.component.ComponentExchange;
import game.component.ComponentPickup;
import game.component.ComponentSemaphore;
import game.component.ComponentSignal;
import game.component.ComponentUnit;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Set;
import java.util.SortedSet;
import java.util.TreeMap;
import java.util.stream.Collectors;
import lgraphs.LGraph;
import lgraphs.ontology.Ontology;
import lgraphs.ontology.Sort;
import lgraphs.sampler.LGraphGrammarSampler;
import lgraphs.sampler.LGraphRewritingGrammar;
import lgraphs.sampler.LGraphRewritingRule;
import support.GameStateExporter;
import support.GameStateParser;
import support.PCG;
import static support.PCG.applyGrammar;
import static support.PCG.embeddGraph;
import util.Sampler;
import static support.PCG.applyGrammar;
import valls.util.MathUtils;

/**
 *
 * @author santi
 */
public class GrammarStatsCurrentLevelSolutions {

    public static final boolean WRITE_ARFF = false;

    public Map<String, Double> processFile(File file) throws Exception {
        GameState gs;
        System.out.println("Processing: " + file.getAbsolutePath());
        gs = GameStateParser.parseFile(file.getAbsolutePath(), false);
        return getStatsOnGameState(gs);
    }

    public void getVisualProperties(GameState gs, Map<String, Double> m) {
        m.put("v_w", (double) gs.getBoardState().getWidth());
        m.put("v_h", (double) gs.getBoardState().getHeight());

        int track_num = 0;
        for(Tile tile:gs.getBoardState().getTiles()){
            if (tile.isPassable()){
                track_num++;
            }
        }
        m.put("v_track_num", (double) track_num);
        double density = (double) track_num / gs.getBoardState().getTiles().length;
        m.put("v_trackDensity", density);
        m.put("v_componentDensity", (double) gs.getComponentState().getComponents().size() / (double) track_num);
        List<Integer> different_components_per_row = new ArrayList();
        List<Integer> same_components_per_row = new ArrayList();
        for (int x = 0; x < gs.getBoardState().getWidth(); x++) {
            Map<String, Integer> cp = new HashMap();
            for (int y = 0; y < gs.getBoardState().getHeight(); y++) {
                for (Component c : gs.getComponentsAt(x, y)) {
                    cp.put(c.getRepresentation(), cp.getOrDefault(c.getRepresentation(), 0) + 1);
                }
            }
            if (cp.size() > 0) {
                same_components_per_row.add(Collections.max(cp.values()));
                different_components_per_row.add(cp.size());
            }
        }
        m.put("v_rowSameComponentsAvg", MathUtils.average(same_components_per_row));
        m.put("v_rowDifferentComponentsAvg", MathUtils.average(different_components_per_row));

        different_components_per_row = new ArrayList();
        same_components_per_row = new ArrayList();
        for (int y = 0; y < gs.getBoardState().getHeight(); y++) {
            Map<String, Integer> cp = new HashMap();
            for (int x = 0; x < gs.getBoardState().getWidth(); x++) {
                for (Component c : gs.getComponentsAt(x, y)) {
                    cp.put(c.getRepresentation(), cp.getOrDefault(c.getRepresentation(), 0) + 1);
                }
            }
            if (cp.size() > 0) {
                same_components_per_row.add(Collections.max(cp.values()));
                different_components_per_row.add(cp.size());
            }
        }
        m.put("v_colSameComponentsAvg", MathUtils.average(same_components_per_row));
        m.put("v_colDifferentComponentsAvg", MathUtils.average(different_components_per_row));

        Set anchors_w = new HashSet();
        Set anchors_h = new HashSet();
        for (Component c : gs.getComponentState().getComponents()) {
            anchors_w.add(c.x);
            anchors_h.add(c.y);
        }
        // from ngo2003modelling
        m.put("v_Simplicity", 3.0 / (double) (anchors_w.size() + anchors_h.size() + gs.getComponentState().getComponents().size()));
        {
            List<List<Tile>> bv = getTracksInVerticalHalves(gs);
            double w0v = bv.get(0).size();
            double w1v = bv.get(1).size();
            double bmv = Math.abs(w0v - w1v) / Math.max(w0v, w1v);
            List<List<Tile>> bh = getTracksInHorizontalHalves(gs);
            double w0h = bh.get(0).size();
            double w1h = bh.get(1).size();
            double bmh = Math.abs(w0h - w1h) / Math.max(w0h, w1h);
            m.put("v_balanceTracks", (double) 1 - (bmv + bmh) / 2);
        }
        {
            List<List<Tile>> bv = getTracksInVerticalHalves(gs);
            //double w0v = bv.get(0).stream().map(i -> i.component_index.size()).reduce(0, Integer::sum);
            //double w1v = bv.get(1).stream().map(i -> i.component_index.size()).reduce(0, Integer::sum);
            double w0v = 0;
            double w1v = 0;
            for(Tile i:bv.get(0)){
                w0v += (double)i.component_index.size();
            }
            for(Tile i:bv.get(1)){
                w1v += (double)i.component_index.size();
            }
            double bmv = Math.abs(w0v - w1v) / Math.max(w0v, w1v);
            List<List<Tile>> bh = getTracksInHorizontalHalves(gs);
            //double w0h = bh.get(0).stream().map(i -> i.component_index.size()).reduce(0, Integer::sum);
            //double w1h = bh.get(1).stream().map(i -> i.component_index.size()).reduce(0, Integer::sum);
            double w0h = 0;
            double w1h = 0;
            for(Tile i:bh.get(0)){
                w0h += (double)i.component_index.size();
            }
            for(Tile i:bh.get(1)){
                w1h += (double)i.component_index.size();
            }
            double bmh = Math.abs(w0h - w1h) / Math.max(w0h, w1h);
            m.put("v_balanceComponents", (double) 1 - (bmv + bmh) / 2);
        }
        {
            List<Double> q_ = new ArrayList();
            for(List<Tile> i : this.getTracksInQuarters(gs)){
                q_.add((double)i.size());
            }
            // q_ is the list of weights, tweak to break ties
            q_.set(0, q_.get(0) + 0.4);
            q_.set(1, q_.get(1) + 0.3);
            q_.set(2, q_.get(2) + 0.2);
            q_.set(3, q_.get(3) + 0.1);
            TreeMap<Double, Integer> t = new TreeMap();
            for (int i = 0; i < 4; i++) {
                t.put(q_.get(i), i);
            }
            List<Map.Entry<Double, Integer>> t_ = new ArrayList(t.entrySet());
            // t_ holds the weights, from smaller to bigger
            int sum = 0;
            for (int i = 0; i < 4; i++) {
                int q = 4 - i;
                int v = 4 - t_.get(i).getValue();
                sum += Math.abs(q - v);
            }
            m.put("v_sequenceTracks", (double)(sum)/8);
        }
        {
            //List<List<Tile>> q__ = this.getTracksInQuarters(gs);
            //List<Double> q_ = q__.stream().map(i -> (double) (i.stream().map(j -> j.component_index.size()).reduce(0, Integer::sum))).collect(Collectors.toList());
            List<Double> q_ = new ArrayList();
            for(List<Tile> i : this.getTracksInQuarters(gs)){
                int sum = 0;
                for(Tile j: i){
                    sum += j.component_index.size();
                }
                q_.add((double)sum);
                
            }
            // q_ is the list of weights, tweak to break ties
            q_.set(0, q_.get(0) + 0.4);
            q_.set(1, q_.get(1) + 0.3);
            q_.set(2, q_.get(2) + 0.2);
            q_.set(3, q_.get(3) + 0.1);
            TreeMap<Double, Integer> t = new TreeMap();
            for (int i = 0; i < 4; i++) {
                t.put(q_.get(i), i);
            }
            List<Map.Entry<Double, Integer>> t_ = new ArrayList(t.entrySet());
            // t_ holds the weights, from smaller to bigger
            int sum = 0;
            for (int i = 0; i < 4; i++) {
                int q = 4 - i;
                int v = 4 - t_.get(i).getValue();
                sum += Math.abs(q - v);
            }
            m.put("v_sequenceComponents", (double) (sum) / 8);
        }

    }

    private List<List<Tile>> getTracksInVerticalHalves(GameState gs) {
        List<Tile> left = new ArrayList();
        List<Tile> right = new ArrayList();
        List<List<Tile>> quadrants = this.getTracksInQuarters(gs);
        left.addAll(quadrants.get(0));
        left.addAll(quadrants.get(2));
        right.addAll(quadrants.get(1));
        right.addAll(quadrants.get(3));
        List<List<Tile>> out = new ArrayList();
        out.add(left);
        out.add(right);
        return out;
    }

    private List<List<Tile>> getTracksInHorizontalHalves(GameState gs) {
        List<Tile> left = new ArrayList();
        List<Tile> right = new ArrayList();
        List<List<Tile>> quadrants = this.getTracksInQuarters(gs);
        left.addAll(quadrants.get(0));
        left.addAll(quadrants.get(1));
        right.addAll(quadrants.get(2));
        right.addAll(quadrants.get(3));
        List<List<Tile>> out = new ArrayList();
        out.add(left);
        out.add(right);
        return out;
    }

    private List<List<Tile>> getTracksInQuarters(GameState gs) {
        // from ngo2003modelling
        // order is as in western reading: UL, UR, LL, LR
        List<List<Tile>> quadrants = new ArrayList();
        int mid_L = gs.getBoardState().getWidth() / 2 + gs.getBoardState().getWidth() % 2;
        int mid_R = gs.getBoardState().getWidth() / 2;
        int mid_T = gs.getBoardState().getHeight() / 2 + gs.getBoardState().getHeight() % 2;
        int mid_B = gs.getBoardState().getHeight() / 2;
        quadrants.add(new ArrayList());
        quadrants.add(new ArrayList());
        quadrants.add(new ArrayList());
        quadrants.add(new ArrayList());
        for (int x = 0; x < gs.getBoardState().getWidth(); x++) {
            for (int y = 0; y < gs.getBoardState().getHeight(); y++) {
                Tile t = gs.getBoardState().getTile(x, y);
                if (!t.isPassable()) {
                    continue;
                }
                if (x < mid_L && y < mid_T) {
                    quadrants.get(0).add(t);
                }
                if (x >= mid_R && y < mid_T) {
                    quadrants.get(1).add(t);
                }
                if (x < mid_L && y >= mid_B) {
                    quadrants.get(2).add(t);
                }
                if (x >= mid_R && y >= mid_B) {
                    quadrants.get(3).add(t);
                }
            }
        }
        return quadrants;
    }

    private void getLevelProperties(GameState gs, Map<String, Double> export_map) {
        export_map.put("num_components", (double) gs.getComponentState().getComponents().size());
        export_map.put("num_semaphores", (double) gs.getComponentState().getComponentsByType(ComponentSemaphore.class).size());
        export_map.put("num_buttons", (double) gs.getComponentState().getComponentsByType(ComponentSignal.class).size());
        export_map.put("num_exchange_points", (double) gs.getComponentState().getComponentsByType(ComponentExchange.class).size());
        export_map.put("num_diverters", (double) gs.getComponentState().getComponentsByType(ComponentDiverter.class).size());
        export_map.put("num_threads", (double) gs.getComponentState().getComponentsByType(ComponentUnit.class).size());
        export_map.put("num_pickups", (double) gs.getComponentState().getComponentsByType(ComponentPickup.class).size());
        export_map.put("num_deliveries", (double) gs.getComponentState().getComponentsByType(ComponentDelivery.class).size());
    }

    public Map<String, Double> getStatsOnGameState(GameState gs) {
        // Collect level statistics
        Map<String, Double> export_map = new HashMap();
        //export_map.putAll(rule_applications);
        // Level properties
        //getLevelProperties(gs, export_map);
        // Visual Properties
        //getVisualProperties(gs, export_map);
        // ME Properties
        GameStateSearch gss = new GameStateSearch(gs);
        gss.setSearchOptions(false, true, true, true);
        gss.setSearchBudget(6000000);
        gss.setSearchTime(100000);
        gss.search();
        gss.getStatsIntoMap(export_map);
        return export_map;

    }

    public void run() throws Exception {

        File out_file = new File("/Users/josepvalls/temp/pppp_samples/", WRITE_ARFF ? "dataset.arff" : "dataset.txt");
        PrintWriter writer = new PrintWriter(out_file);
        Map<String, Double> row;

        List<String> keys = null;

        String path = "/Users/josepvalls/paraprog/Assets/Resources/Levels";
        String path2 = "/Users/josepvalls/paraprog/solutions";
        for (int level = 1; level < 10; level++) {
        //for (int level = 5; level < 6; level++) {
            System.out.println("LEVEL" + level);
            File file = new File(path, "level0" + level + ".txt");
            row = processFile(file);
            
            if (keys == null) {
                // prepare the header
                if(WRITE_ARFF){
                    writer.print("@RELATION pppp\n");
                }
                keys = new ArrayList(row.keySet());
                java.util.Collections.sort(keys);
                for (int j = 0; j < keys.size(); j++) {
                    if(WRITE_ARFF){
                        writer.print("@ATTRIBUTE ");
                        writer.print(keys.get(j));
                        writer.print(" NUMERIC\n");
                    } else {
                        writer.print(keys.get(j));
                        writer.print("\t");
                    }
                }
                if(WRITE_ARFF){
                    writer.print("@ATTRIBUTE class {PCG,Manual}\n");
                    writer.print("@DATA\n");
                } else {
                    writer.print("\n");
                }
            }
            
            for (int j = 0; j < keys.size(); j++) {
                writer.print(row.get(keys.get(j)));
                if(WRITE_ARFF){
                    writer.print(',');
                } else {
                    writer.print('\t');
                }
            }
            writer.print("Manual");
            writer.print('\n');
            
            file = new File(path2, "level" + level + "_expectedSolution.txt");
            row = processFile(file);
            for (int j = 0; j < keys.size(); j++) {
                writer.print(row.get(keys.get(j)));
                if(WRITE_ARFF){
                    writer.print(',');
                } else {
                    writer.print('\t');
                }
            }
            writer.print("Solved");
            writer.print('\n');

        }
        writer.close();
    }

    public static void main(String args[]) throws Exception {
        GameStateParser.prepareParser(); // we need the component names
        GrammarStatsCurrentLevelSolutions gs = new GrammarStatsCurrentLevelSolutions();
        gs.run();
    }
}
