using System;
using System.Collections.Generic;
using UnityEngine;
using static ROTanks.ROTLog;

namespace ROTanks
{

    public static class ROTModelLayout
    {

        private static Dictionary<string, ModelLayoutData> layouts = new Dictionary<string, ModelLayoutData>();

        private static bool loaded = false;

        public static void load()
        {
            log("Loading Model Layouts");
            layouts.Clear();
            ConfigNode[] layoutNodes = GameDatabase.Instance.GetConfigNodes("ROT_MODEL_LAYOUT");
            int len = layoutNodes.Length;
            for (int i = 0; i < len; i++)
            {
                ModelLayoutData mld = new ModelLayoutData(layoutNodes[i]);
                layouts.Add(mld.name, mld);
            }
            loaded = true;
            log("Finished loading Model Layouts");
        }

        public static ModelLayoutData findLayout(string name)
        {
            if (!loaded)
            {
                load();
            }
            ModelLayoutData mld;
            if (!layouts.TryGetValue(name, out mld))
            {
                error("Could not find layout by name: " + name);
            }
            return mld;
        }

        public static ModelLayoutData[] findLayouts(string[] names)
        {
            int len = names.Length;
            ModelLayoutData[] mlds = new ModelLayoutData[len];
            for (int i = 0; i < len; i++)
            {
                mlds[i] = findLayout(names[i]);
            }
            return mlds;
        }

    }

    /// <summary>
    /// Data that defines how a single model is positioned inside a ModelModule when more than one internal model is desired.
    /// This position/scale/rotation data is applied to sub-models in addition to the base ModelModule position/scale data.
    /// </summary>
    public struct ModelPositionData
    {

        /// <summary>
        /// The local position of a single model, relative to the model-modules position
        /// </summary>
        public readonly Vector3 localPosition;

        /// <summary>
        /// The local scale of a single model, relative to the model-modules scale
        /// </summary>
        public readonly Vector3 localScale;

        /// <summary>
        /// The local rotation to be applied to a single model (euler x,y,z)
        /// </summary>
        public readonly Vector3 localRotation;

        public ModelPositionData(ConfigNode node)
        {
            localPosition = node.GetVector3("position", Vector3.zero);
            localScale = node.GetVector3("scale", Vector3.one);
            localRotation = node.GetVector3("rotation", Vector3.zero);
        }

        public ModelPositionData(Vector3 pos, Vector3 scale, Vector3 rotation)
        {
            localPosition = pos;
            localScale = scale;
            localRotation = rotation;
        }

        public Vector3 scaledPosition(Vector3 scale)
        {
            return Vector3.Scale(localPosition, scale);
        }

        public Vector3 scaledPosition(float scale)
        {
            return localPosition * scale;
        }

        public Vector3 scaledScale(Vector3 scale)
        {
            return Vector3.Scale(localScale, scale);
        }

        public Vector3 scaledScale(float scale)
        {
            return localScale * scale;
        }

    }

    /// <summary>
    /// Named model position layout data class.  Used to store positions of models, for use in ModelModule setup.<para/>
    /// Defined independently of the models that may use them, stored and referenced globally/game-wide.<para/>
    /// A single ModelLayoutData may be used by multiple ModelDefinitions, and a single ModelDefinition may have different ModelLayoutDatas applied to it by the controlling part-module.
    /// </summary>
    public class ModelLayoutData
    {

        public readonly string name;
        public readonly string title;
        public readonly ModelPositionData[] positions;
        public ModelLayoutData(ConfigNode node)
        {
            name = node.GetStringValue("name");
            title = node.GetStringValue("title", name);
            ConfigNode[] posNodes = node.GetNodes("POSITION");
            int len = posNodes.Length;
            positions = new ModelPositionData[len];
            for (int i = 0; i < len; i++)
            {
                positions[i] = new ModelPositionData(posNodes[i]);
            }
        }

        public ModelLayoutData(string name, ModelPositionData[] positions)
        {
            this.name = name;
            this.positions = positions;
        }

        /// <summary>
        /// Returns the average of the magnitude of the scale vectors applied to each position.  Used for adjusting thrust/etc on multi-position model setups.
        /// </summary>
        /// <returns></returns>
        public float modelScalarAverage()
        {
            float pa = 0;
            int len = positions.Length;
            if (len > 0)
            {
                for (int i = 0; i < len; i++)
                {
                    pa += positions[i].localScale.magnitude;
                }
                pa /= len;
            }
            return pa;
        }

    }

    /// <summary>
    /// Container class to wrap a global model definition and its part-specific layout options.
    /// </summary>
    public class ModelDefinitionLayoutOptions
    {

        public readonly ROTModelDefinition definition;
        public readonly ModelLayoutData[] layouts;

        public ModelDefinitionLayoutOptions(ROTModelDefinition def)
        {
            definition = def;
            if (definition == null) { error("Model definition was null when creating model layout options!"); }
            layouts = ROTModelLayout.findLayouts(new string[] { "default" });
            if (this.layouts == null || this.layouts.Length < 1)
            {
                throw new InvalidOperationException("ERROR: No valid layout data specified.");
            }
        }

        public ModelDefinitionLayoutOptions(ROTModelDefinition def, ModelLayoutData[] layouts)
        {
            this.definition = def;
            if (definition == null) { error("Model definition was null when creating model layout options!"); }
            this.layouts = layouts;
            if (this.layouts == null || this.layouts.Length < 1)
            {
                throw new InvalidOperationException("ERROR: No valid layout data specified.");
            }
        }

        public ModelLayoutData getLayout(string name)
        {
            ModelLayoutData mld = layouts.Find(m => m.name == name);
            if (mld == null)
            {
                error("ERROR: Could not locate layout for name: " + name);
            }
            return mld;
        }

        public ModelLayoutData getDefaultLayout()
        {
            return layouts[0];
        }

        public bool isValidLayout(string name)
        {
            return layouts.Exists(m => m.name == name);
        }

        public string[] getLayoutNames()
        {
            return ROTUtils.getNames(layouts, m => m.name);
        }

        public string[] getLayoutTitles()
        {
            return ROTUtils.getNames(layouts, m => m.title);
        }

        public override string ToString()
        {
            return "Model Definition Layout: " + definition + ":[" + layouts.Length+"]";
        }

    }

}
