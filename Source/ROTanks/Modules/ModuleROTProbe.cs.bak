using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSPShaderTools;
using ROLib;

namespace ROTanks
{
    public class ModuleROTProbe : PartModule, IRecolorable, IContainerVolumeContributor, IPartMassModifier
    {
        #region KSPFields

        [KSPField]
        public float diameterLargeStep = 0.1f;

        [KSPField]
        public float diameterSmallStep = 0.1f;

        [KSPField]
        public float diameterSlideStep = 0.001f;

        [KSPField]
        public float minDiameter = 0.1f;

        [KSPField]
        public float maxDiameter = 5.0f;

        [KSPField]
        public float volumeScalingPower = 3f;

        [KSPField]
        public float massScalingPower = 3f;

        [KSPField]
        public bool enableVScale = true;

        [KSPField]
        public int coreContainerIndex = 0;

        [KSPField]
        public int auxContainerSourceIndex = -1;

        [KSPField]
        public int auxContainerTargetIndex = -1;

        [KSPField]
        public float auxContainerMinPercent = 0f;

        [KSPField]
        public float auxContainerMaxPercent = 0f;

        [KSPField]
        public string coreManagedNodes = string.Empty;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Total Length", guiFormat = "F4", guiUnits = "m")]
        public float totalTankLength = 0.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Largest Diameter", guiFormat = "F4", guiUnits = "m")]
        public float largestDiameter = 0.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Diameter", guiUnits = "m"),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true)]
        public float currentDiameter = 1.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "V.ScaleAdj"),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = -1, maxValue = 1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f)]
        public float currentVScale = 0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Support", guiUnits = "%"),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = 0, maxValue = 15, incrementLarge = 5, incrementSmall = 1, incrementSlide = 0.1f)]
        public float auxContainerPercent = 0f;

        [KSPField(isPersistant = true, guiName = "Variant", guiActiveEditor = true, guiActive = false),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentVariant = "Default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Core"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentCore = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Core Tex"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentCoreTexture = "default";

        [KSPField(isPersistant = true)]
        public string coreModulePersistentData = string.Empty;

        [KSPField(isPersistant = true)]
        public bool initializedDefaults = false;

        #endregion KSPFields


        #region Private Variables

        [Persistent]
        public string configNodeData = string.Empty;

        private bool initialized = false;
        private float modifiedMass = -1;
        private float prevDiameter = -1;
        private string[] coreNodeNames;
        private ROLModelModule<ModuleROTProbe> coreModule;

        /// <summary>
        /// Mapping of all of the variant sets available for this part.  When variant list length > 0, an additional 'variant' UI slider is added to allow for switching between variants.
        /// </summary>
        private Dictionary<string, ModelDefinitionVariantSet> variantSets = new Dictionary<string, ModelDefinitionVariantSet>();

        /// <summary>
        /// Helper method to get or create a variant set for the input variant name.  If no set currently exists, a new set is empty set is created and returned.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ModelDefinitionVariantSet getVariantSet(string name)
        {
            ModelDefinitionVariantSet set = null;
            if (!variantSets.TryGetValue(name, out set))
            {
                set = new ModelDefinitionVariantSet(name);
                variantSets.Add(name, set);
            }
            return set;
        }

        /// <summary>
        /// Helper method to find the variant set for the input model definition.  Will nullref/error if no variant set is found.  Will NOT create a new set if not found.
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        private ModelDefinitionVariantSet getVariantSet(ModelDefinitionLayoutOptions def)
        {
            //returns the first variant set out of all variants where the variants definitions contains the input definition
            return variantSets.Values.Where((a, b) => { return a.definitions.Contains(def); }).First();
        }

        #endregion Private Variables


        #region KSP Overrides

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
            initialize();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            initialize();
            initializeUI();
            updateMassAndDimensions();
        }

        public void Start()
        {
            initializedDefaults = true;
            updateDragCubes();
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Remove(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }
        }

        private void onEditorVesselModified(ShipConstruct ship)
        {
            //update available variants for attach node changes
            updateAvailableVariants();
        }

        // IPartMass Override
        public ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }

        // IPartMass Override
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (modifiedMass == -1) { return 0; }
            return -defaultMass + modifiedMass;
        }

        // IRecolorable Override
        public string[] getSectionNames()
        {
            return new string[] { "Core" };
        }

        // IRecolorable Override
        public RecoloringData[] getSectionColors(string section)
        {
            return coreModule.recoloringData;
        }

        // IRecolorable Override
        public void setSectionColors(string section, RecoloringData[] colors)
        {
            coreModule.setSectionColors(colors);
        }

        // IRecolorable Override
        public TextureSet getSectionTexture(string section)
        {
            return coreModule.textureSet;
        }

        // IContainerVolumeContributor Override
        public ContainerContribution[] getContainerContributions()
        {
            ContainerContribution[] cts;
            float auxVol = 0;
            ContainerContribution ctCore = getCC("core", coreContainerIndex, coreModule.moduleVolume * 1000f, ref auxVol);
            ContainerContribution ctAux = new ContainerContribution("aux", auxContainerTargetIndex, auxVol);
            cts = new ContainerContribution[2] { ctCore, ctAux };
            return cts;
        }

        private ContainerContribution getCC(string name, int index, float vol, ref float auxVol)
        {
            float ap = auxContainerPercent * 0.01f;
            float contVol = vol;
            if (index == auxContainerSourceIndex && auxContainerTargetIndex >= 0)
            {
                auxVol += vol * ap;
                contVol = (1 - ap) * vol;
            }
            return new ContainerContribution(name, index, contVol);
        }

        #endregion KSP Overrides


        #region Custom Update Methods

        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;

            prevDiameter = currentDiameter;

            coreNodeNames = ROLUtils.parseCSV(coreManagedNodes);

            //model-module setup/initialization
            ConfigNode node = ROLConfigNodeUtils.parseConfigNode(configNodeData);

            //list of CORE model nodes from config
            //each one may contain multiple 'model=modelDefinitionName' entries
            //but must contain no more than a single 'variant' entry.
            //if no variant is specified, they are added to the 'Default' variant.
            ConfigNode[] coreDefNodes = node.GetNodes("CORE");
            ModelDefinitionLayoutOptions[] coreDefs;
            List<ModelDefinitionLayoutOptions> coreDefList = new List<ModelDefinitionLayoutOptions>();
            int coreDefLen = coreDefNodes.Length;
            for (int i = 0; i < coreDefLen; i++)
            {
                string variantName = coreDefNodes[i].ROLGetStringValue("variant", "Default");
                coreDefs = ROLModelData.getModelDefinitionLayouts(coreDefNodes[i].ROLGetStringValues("model"));
                coreDefList.AddUniqueRange(coreDefs);
                ModelDefinitionVariantSet mdvs = getVariantSet(variantName);
                mdvs.addModels(coreDefs);
            }
            coreDefs = coreDefList.ToArray();

            coreModule = new ROLModelModule<ModuleROTProbe>(part, this, getRootTransform("ModularProbe-CORE"), ModelOrientation.CENTRAL, nameof(currentCore), null, nameof(currentCoreTexture), nameof(coreModulePersistentData));
            coreModule.name = "ModularProbe-Core";
            coreModule.getSymmetryModule = m => m.coreModule;
            coreModule.getValidOptions = () => getVariantSet(currentVariant).definitions;

            coreModule.massScalar = massScalingPower;
            coreModule.volumeScalar = volumeScalingPower;

            //set up the model lists and load the currently selected model
            coreModule.setupModelList(coreDefs);
            coreModule.setupModel();

            updateModulePositions();
            updateMassAndDimensions();
            updateAttachNodes(false);
            updateAvailableVariants();
            ROLStockInterop.updatePartHighlighting(part);
        }

        /// <summary>
        /// Initialize the UI controls, including default values, and specifying delegates for their 'onClick' methods.<para/>
        /// All UI based interaction code will be defined/run through these delegates.
        /// </summary>
        private void initializeUI()
        {
            Action<ModuleROTProbe> modelChangedAction = (m) =>
            {
                m.updateModulePositions();
                m.updateMassAndDimensions();
                m.updateAttachNodes(true);
                m.updateAvailableVariants();
                m.updateDragCubes();
                ROLModInterop.updateResourceVolume(m.part);
            };

            //set up the core variant UI control
            string[] variantNames = ROLUtils.getNames(variantSets.Values, m => m.variantName);
            this.ROLupdateUIChooseOptionControl(nameof(currentVariant), variantNames, variantNames, true, currentVariant);
            Fields[nameof(currentVariant)].guiActiveEditor = variantSets.Count > 1;

            Fields[nameof(currentVariant)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                //TODO find variant set for the currently enabled core model
                //query the index from that variant set
                ModelDefinitionVariantSet prevMdvs = getVariantSet(coreModule.definition.name);
                //this is the index of the currently selected model within its variant set
                int previousIndex = prevMdvs.indexOf(coreModule.layoutOptions);
                //grab ref to the current/new variant set
                ModelDefinitionVariantSet mdvs = getVariantSet(currentVariant);
                //and a reference to the model from same index out of the new set ([] call does validation internally for IAOOBE)
                ModelDefinitionLayoutOptions newCoreDef = mdvs[previousIndex];
                //now, call model-selected on the core model to update for the changes, including symmetry counterpart updating.
                this.ROLactionWithSymmetry(m =>
                {
                    m.currentVariant = currentVariant;
                    m.coreModule.modelSelected(newCoreDef.definition.name);
                    modelChangedAction(m);
                });
            };

            Fields[nameof(currentDiameter)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                this.ROLactionWithSymmetry(m =>
                {
                    if (m != this) { m.currentDiameter = this.currentDiameter; }
                    modelChangedAction(m);
                    m.prevDiameter = m.currentDiameter;
                });
                ROLStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentVScale)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                this.ROLactionWithSymmetry(m =>
                {
                    if (m != this) { m.currentVScale = this.currentVScale; }
                    modelChangedAction(m);
                });
                ROLStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentCore)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                coreModule.modelSelected(a, b);
                this.ROLactionWithSymmetry(modelChangedAction);
                ROLStockInterop.fireEditorUpdate();
            };

            //------------------MODEL DIAMETER SWITCH UI INIT---------------------//
            if (maxDiameter == minDiameter)
            {
                Fields[nameof(currentDiameter)].guiActiveEditor = false;
            }
            else
            {
                this.ROLupdateUIFloatEditControl(nameof(currentDiameter), minDiameter, maxDiameter, diameterLargeStep, diameterSmallStep, diameterSlideStep, true, currentDiameter);
            }
            Fields[nameof(currentVScale)].guiActiveEditor = enableVScale;

            //------------------AUX CONTAINER SWITCH UI INIT---------------------//
            Fields[nameof(auxContainerPercent)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                this.ROLactionWithSymmetry(m =>
                {
                    if (m != this) { m.auxContainerPercent = this.auxContainerPercent; }
                    ROLModInterop.updateResourceVolume(m.part);
                    ROLStockInterop.fireEditorUpdate();
                });
            };
            if (auxContainerMinPercent == auxContainerMaxPercent || auxContainerSourceIndex < 0 || auxContainerTargetIndex < 0)
            {
                Fields[nameof(auxContainerPercent)].guiActiveEditor = false;
            }
            else
            {
                this.ROLupdateUIFloatEditControl(nameof(auxContainerPercent), auxContainerMinPercent, auxContainerMaxPercent, 5f, 1f, 0.1f, false, auxContainerPercent);
            }

            //------------------MODULE TEXTURE SWITCH UI INIT---------------------//
            Fields[nameof(currentCoreTexture)].uiControlEditor.onFieldChanged = coreModule.textureSetSelected;

            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }
        }

        public void updateAvionicsData()
        {
            PartModule avionicsData;
            avionicsData = part.Modules["ProceduralAvionics"];
            avionicsData.GetType().GetField("cachedVolume").SetValue(avionicsData, 1000.0f);
        }

        /// <summary>
        /// Update the scale and position values for all currently configured models.  Does no validation, only updates positions.<para/>
        /// After calling this method, all models will be scaled and positioned according to their internal position/scale values and the orientations/offsets defined in the models.
        /// </summary>
        private void updateModulePositions()
        {
            //scales for modules depend on the module above/below them
            //first set the scale for the core module -- this depends directly on the UI specified 'diameter' value.
            coreModule.setScaleForDiameter(currentDiameter, currentVScale);

            //total height of the part is determined by the sum of the heights of the modules at their current scale
            float totalHeight = coreModule.moduleHeight;

            //position of each module is set such that the vertical center of the models is at part origin/COM
            float pos = totalHeight * 0.5f; //abs top of model
            pos -= coreModule.moduleHeight * 0.5f; //center of 'core' model
            coreModule.setPosition(pos);
            pos -= coreModule.moduleHeight * 0.5f; //bottom of 'core' model

            //update actual model positions and scales
            coreModule.updateModelMeshes();
        }

        /// <summary>
        /// Update the cached modifiedMass and dimension updates for the PAW. Used with stock cost/mass modifier interface.
        /// </summary>
        private void updateMassAndDimensions()
        {
            modifiedMass = coreModule.moduleMass;
            totalTankLength = getTotalHeight();
        }

        /// <summary>
        /// Update the attach nodes for the current model-module configuration. 
        /// The 'nose' module is responsible for updating of upper attach nodes, while the 'mount' module is responsible for lower attach nodes.
        /// Also includes updating of 'interstage' nose/mount attach nodes.
        /// Also includes updating of surface-attach node position.
        /// Also includes updating of any parts that are surface attached to this part.
        /// </summary>
        /// <param name="userInput"></param>
        private void updateAttachNodes(bool userInput)
        {
            //update the standard top and bottom attach nodes, using the node position(s) defined in the nose and mount modules
            coreModule.updateAttachNodeTop("top", userInput);
            coreModule.updateAttachNodeBottom("bottom", userInput);

            //update the model-module specific attach nodes, using the per-module node definitions from the part
            coreModule.updateAttachNodeBody(coreNodeNames, userInput);

            //update surface attach node position, part position, and any surface attached children
            //TODO -- how to determine how far to offset/move surface attached children?
            AttachNode surfaceNode = part.srfAttachNode;
            if (surfaceNode != null)
            {
                coreModule.updateSurfaceAttachNode(surfaceNode, prevDiameter, userInput);
            }
        }

        /// <summary>
        /// Return the total height of this part in its current configuration.  This will be the distance from the bottom attach node to the top attach node, and may not include any 'extra' structure. TOOLING
        /// </summary>
        /// <returns></returns>
        private float getTotalHeight()
        {
            float totalHeight = coreModule.moduleHeight;
            return totalHeight;
        }

        /// <summary>
        /// Return the topmost position in the models relative to the part's origin.
        /// </summary>
        /// <returns></returns>
        private float getPartTopY()
        {
            return getTotalHeight() * 0.5f;
        }

        /// <summary>
        /// Update the UI visibility for the currently available selections.<para/>
        /// Will hide/remove UI fields for slots with only a single option (models, textures, layouts).
        /// </summary>
        private void updateAvailableVariants()
        {
            coreModule.updateSelections();
        }

        /// <summary>
        /// Calls the generic SSTU procedural drag-cube updating routines.  Will update the drag cubes for whatever the current model state is.
        /// </summary>
        private void updateDragCubes()
        {
            ROLModInterop.onPartGeometryUpdate(part, true);
        }

        /// <summary>
        /// Return the root transform for the specified name.  If does not exist, will create it and parent it to the parts' 'model' transform.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="recreate"></param>
        /// <returns></returns>
        private Transform getRootTransform(string name)
        {
            Transform root = part.transform.ROLFindRecursive(name);
            if (root != null)
            {
                GameObject.DestroyImmediate(root.gameObject);
                root = null;
            }
            root = new GameObject(name).transform;
            root.NestToParent(part.transform.ROLFindRecursive("model"));
            return root;
        }

        /// <summary>
        /// Return the model-module corresponding to the input slot name.  Valid slot names are: NOSE,UPPER,CORE,LOWER,MOUNT
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ROLModelModule<ModuleROTProbe> getModuleByName(string name)
        {
            switch (name)
            {
                case "CORE":
                    return coreModule;
                default:
                    return null;
            }
        }

        #endregion Custom Update Methods


        /// <summary>
        /// Data storage for a group of model definitions that share the same 'variant' type.  Used by modular-part in variant-defined configurations.
        /// </summary>
        public class ModelDefinitionVariantSet
        {
            public readonly string variantName;

            public ModelDefinitionLayoutOptions[] definitions = new ModelDefinitionLayoutOptions[0];

            public ModelDefinitionLayoutOptions this[int index]
            {
                get
                {
                    if (index < 0) { index = 0; }
                    if (index >= definitions.Length) { index = definitions.Length - 1; }
                    return definitions[index];
                }
            }

            public ModelDefinitionVariantSet(string name)
            {
                this.variantName = name;
            }

            public void addModels(ModelDefinitionLayoutOptions[] defs)
            {
                List<ModelDefinitionLayoutOptions> allDefs = new List<ModelDefinitionLayoutOptions>();
                allDefs.AddRange(definitions);
                allDefs.AddUniqueRange(defs);
                definitions = allDefs.ToArray();
            }

            public int indexOf(ModelDefinitionLayoutOptions def)
            {
                return definitions.IndexOf(def);
            }

        }
    }
}
