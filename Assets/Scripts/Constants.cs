﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Constants {
    public static class FilePrefixes
    {
        public const string inputLevelFile = "MEInput"; //+.txt after saving
        public const string outputLevelFile = "MEOutput";
    }

	public static class ComponentSortingOrder
	{
		public const int background = 0;
		public const int track = 1;
		public const int basicComponents = 2;
		public const int thread = 3;
		public const int connectionOverlay = 4;
		public const int connectionComponents = 5;
		public const int thread_simulation = 6;
		public const int fullOverlay = 10;
	}

    public static class ComponentLinkColor
    {
        public static readonly Dictionary<string, List<Color>> componentLinkColors = new Dictionary<string, List<Color>>()
        {
            { "default", new List<Color>() { Color.white }  },
            { "diverter", new List<Color>() { new Color(0.5f,0.8f,0.3f) } },
            { "directional", new List<Color>() { new Color(0.8f,0.8f,1f) } },
            { "semaphore", new List<Color>() { new Color(0.882f, 0.960f, 0.996f), new Color(0.701f, 0.898f, 0.988f), new Color(0.309f, 0.764f, 0.968f), new Color(0.011f, 0.662f, 0.956f), new Color(0.007f, 0.533f, 0.819f), new Color(0.003f, 0.341f, 0.607f), new Color(0.003f, 0.274f, 0.419f), new Color(0.015f, 0.164f, 0.243f)} },
            { "conditional", new List<Color>() { new Color(0.988f, 0.890f, 0.921f), new Color(0.968f, 0.725f, 0.807f), new Color(0.952f, 0.552f, 0.690f), new Color(0.941f, 0.376f, 0.564f), new Color(0.917f, 0.094f, 0.380f), new Color(0.764f, 0.117f, 0.360f), new Color(0.537f, 0.101f, 0.317f), new Color(0.352f, 0.058f, 0.235f)} }
        };
    }

    public static class LinkColors
    {
        public static readonly List<Color> fullColorSet = new List<Color>()
        {
            new Color(242f/255f,243f/255f,244/255f),
            new Color(34f/255f,34f/255f,34f/255f),
            new Color(243f/255f,195f/255f,0f/255f),
            new Color(135f/255f,86f/255f,146f/255f),
            new Color(243f/255f,86f/255f,146f/255f),
            new Color(161f/255f,202f/255f,241f/255f),
            new Color(190f/255f,0f/255f,50f/255f),
            new Color(194f/255f,178f/255f,128f/255f),
            new Color(132f/255f,132f/255f,132f/255f),
            new Color(0, 0.533f, 0.337f),
            new Color(0.901f, 0.560f, 0.674f),
            new Color(0, 0.403f, 0.647f),
            new Color(0.976f, 0.576f, 0.474f),
            new Color(0.376f, 0.305f, 0.592f),
            new Color(0.701f, 0.266f, 0.423f),
            new Color(0.862f, 0.827f, 0f),
            new Color(0.533f, 0.176f, 0.090f),
            new Color(0.552f, 0.713f, 0f),
            new Color(0.396f, 0.270f, 0.133f),
            new Color(0.886f, 0.345f, 0.133f),
            new Color(0.168f, 0.239f, 0.149f)

        };
    }

    public static class SimulationValues
    {
        public static readonly Dictionary<string, float> AnimationFactors = new Dictionary<string, float>()
        {
            { "default", 1.5f },
            { "default", 1.5f }
        };
    }

    public static class CameraZoomValues
    {
        public static float WidthPriority = 0.57f;
        public static float HeightPriority = 0.71f;
    }
}
