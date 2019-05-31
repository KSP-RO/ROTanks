using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSPShaderTools;

namespace ROTanks
{

    /// <summary>
    /// PartModule that manages multiple models/meshes and accompanying features for model switching - resources, modules, textures, recoloring.<para/>
    /// Includes 3 stack-mounted modules.  All modules support model-switching, texture-switching, recoloring.
    /// </summary>
    public class ModuleROTank : PartModule, IPartCostModifier, IPartMassModifier, IRecolorable, IContainerVolumeContributor
    {

        #region REGION - Part Config Fields

        [KSPField]
        public float diameterLargeStep = 0.1f;

        [KSPField]
        public float diameterSmallStep = 0.1f;

        [KSPField]
        public float diameterSlideStep = 0.001f;

        [KSPField]
        public float minDiameter = 0.1f;

        [KSPField]
        public float maxDiameter = 50f;

        [KSPField]
        public float volumeScalingPower = 3f;

        [KSPField]
        public float massScalingPower = 3f;

        [KSPField]
        public bool enableVScale = true;

        [KSPField]
        public bool useAdapterMass = true;

        [KSPField]
        public bool useAdapterCost = true;

        [KSPField]
        public int noseContainerIndex = 0;

        [KSPField]
        public int coreContainerIndex = 0;

        [KSPField]
        public int mountContainerIndex = 0;

        [KSPField]
        public int topFairingIndex = -1;

        [KSPField]
        public int centralFairingIndex = -1;

        [KSPField]
        public int bottomFairingIndex = -1;

        [KSPField]
        public string noseManagedNodes = string.Empty;

        [KSPField]
        public string coreManagedNodes = string.Empty;

        [KSPField]
        public string mountManagedNodes = string.Empty;

        [KSPField]
        public float actualHeight = 0.0f;


        /// <summary>
        /// Name of the 'interstage' node; positioned according to upper fairing lower spawn point
        /// </summary>
        [KSPField]
        public string noseInterstageNode = "noseinterstage";

        /// <summary>
        /// Name of the 'interstage' node; positioned according to lower fairing upper spawn point
        /// </summary>
        [KSPField]
        public string mountInterstageNode = "mountinterstage";

        /// <summary>
        /// This is the total length of the entire tank with the additional modules added.
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Total Length", guiFormat = "F4", guiUnits = "m")]
        public float totalTankLength = 0.0f;
        
        /// <summary>
        /// This is the largest diameter of the entire tank.
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Largest Diameter", guiFormat = "F4", guiUnits = "m")]
        public float largestDiameter = 0.0f;

        /// <summary>
        /// The current user selected diamater of the part.  Drives the scaling and positioning of everything else in the model.
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Diameter", guiUnits = "m"),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true)]
        public float currentDiameter = 1.0f;    

        /// <summary>
        /// Adjustment to the vertical-scale of v-scale compatible models/module-slots.
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "V.ScaleAdj"),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = -1, maxValue = 1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f)]
        public float currentVScale = 0f;

        #region REGION - Module persistent data fields

        //------------------------------------------MODEL SELECTION SET PERSISTENCE-----------------------------------------------//

        //non-persistent value; initialized to whatever the currently selected core model definition is at time of loading
        //allows for variant names to be updated in the part-config without breaking everything....
        [KSPField(isPersistant =true, guiName = "Variant", guiActiveEditor = true, guiActive = false),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentVariant = "Default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Nose"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentNose = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Core"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentCore = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Mount"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentMount = "Mount-None";

        //------------------------------------------TEXTURE SET PERSISTENCE-----------------------------------------------//

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Nose Tex"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentNoseTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Core Tex"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentCoreTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Mount Tex"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentMountTexture = "default";

        //------------------------------------------RECOLORING PERSISTENCE-----------------------------------------------//

        //persistent data for modules; stores colors
        [KSPField(isPersistant = true)]
        public string noseModulePersistentData = string.Empty;

        [KSPField(isPersistant = true)]
        public string coreModulePersistentData = string.Empty;

        [KSPField(isPersistant = true)]
        public string mountModulePersistentData = string.Empty;

        #endregion ENDREGION - Module persistent data fields

        //tracks if default textures and resource volumes have been initialized; only occurs once during the parts' first Start() call
        [KSPField(isPersistant = true)]
        public bool initializedDefaults = false;

        #endregion REGION - Part Config Fields

        #region REGION - Private working vars

        /// <summary>
        /// Standard work-around for lack of config-node data being passed consistently and lack of support for mod-added serializable classes.
        /// </summary>
        [Persistent]
        public string configNodeData = string.Empty;

        /// <summary>
        /// Has initialization been run?  Set to true the first time init methods are run (OnLoad/OnStart), and ensures that init is only run a single time.
        /// </summary>
        private bool initialized = false;

        /// <summary>
        /// The adjusted modified mass for this part.
        /// </summary>
        private float modifiedMass = -1;

        /// <summary>
        /// The adjusted modified cost for this part.
        /// </summary>
        private float modifiedCost = -1;

        /// <summary>
        /// Previous diameter value, used for surface attach position updates.
        /// </summary>
        private float prevDiameter = -1;

        private string[] noseNodeNames;
        private string[] coreNodeNames;
        private string[] mountNodeNames;

        //Main module slots for nose/core/mount
        private ROTModelModule<ModuleROTank> noseModule;
        private ROTModelModule<ModuleROTank> coreModule;
        private ROTModelModule<ModuleROTank> mountModule;

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

        #endregion ENDREGION - Private working vars

        #region REGION - Standard KSP Overrides

        //standard KSP lifecyle override
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
            initialize();
        }

        //standard KSP lifecyle override
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            initialize();
            initializeUI();
        }

        //standard Unity lifecyle override
        public void Start()
        {
            if (!initializedDefaults)
            {
                updateFairing(false);
            }
            initializedDefaults = true;
            updateDragCubes();
        }

        //standard Unity lifecyle override
        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Remove(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }
        }

        //KSP editor modified event callback
        private void onEditorVesselModified(ShipConstruct ship)
        {
            //update available variants for attach node changes
            updateAvailableVariants();
        }

        //IPartMass/CostModifier override
        public ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }

        //IPartMass/CostModifier override
        public ModifierChangeWhen GetModuleCostChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }

        //IPartMass/CostModifier override
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (modifiedMass == -1) { return 0; }
            return -defaultMass + modifiedMass;
        }

        //IPartMass/CostModifier override
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            if (modifiedCost == -1) { return 0; }
            return -defaultCost + modifiedCost;
        }

        //IRecolorable override
        public string[] getSectionNames()
        {
            return new string[] { "Nose", "Core", "Mount" };
        }

        //IRecolorable override
        public RecoloringData[] getSectionColors(string section)
        {
            if (section == "Nose")
            {
                return noseModule.recoloringData;
            }
            else if (section == "Core")
            {
                return coreModule.recoloringData;
            }
            else if (section == "Mount")
            {
                return mountModule.recoloringData;
            }
            return coreModule.recoloringData;
        }

        //IRecolorable override
        public void setSectionColors(string section, RecoloringData[] colors)
        {
            if (section == "Nose")
            {
                noseModule.setSectionColors(colors);
            }
            else if (section == "Core")
            {
                coreModule.setSectionColors(colors);
            }
            else if (section == "Mount")
            {
                mountModule.setSectionColors(colors);
            }
        }

        //IRecolorable override
        public TextureSet getSectionTexture(string section)
        {
            if (section == "Nose")
            {
                return noseModule.textureSet;
            }
            else if (section == "Core")
            {
                return coreModule.textureSet;
            }
            else if (section == "Mount")
            {
                return mountModule.textureSet;
            }
            return coreModule.textureSet;
        }

        //IContainerVolumeContributor override
        public ContainerContribution[] getContainerContributions()
        {
            ContainerContribution[] cts;
            ContainerContribution ct0 = getCC("nose", noseContainerIndex, noseModule.moduleVolume * 1000f);
            ContainerContribution ct1 = getCC("core", coreContainerIndex, coreModule.moduleVolume * 1000f);
            ContainerContribution ct2 = getCC("mount", mountContainerIndex, mountModule.moduleVolume * 1000f);
            cts = new ContainerContribution[3] { ct0, ct1, ct2 };
            return cts;
        }

        private ContainerContribution getCC(string name, int index, float vol)
        {
            float contVol = vol;
            return new ContainerContribution(name, index, contVol);
        }

        #endregion ENDREGION - Standard KSP Overrides

        #region REGION - Custom Update Methods

        /// <summary>
        /// Initialization method.  Sets up model modules, loads their configs from the input config node.  Does all initial linking of part-modules.<para/>
        /// Does NOT set up their UI interaction -- that is all handled during OnStart()
        /// </summary>
        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;

            prevDiameter = currentDiameter;

            noseNodeNames = ROTUtils.parseCSV(noseManagedNodes);
            coreNodeNames = ROTUtils.parseCSV(coreManagedNodes);
            mountNodeNames = ROTUtils.parseCSV(mountManagedNodes);

            //model-module setup/initialization
            ConfigNode node = ROTConfigNodeUtils.parseConfigNode(configNodeData);

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
                string variantName = coreDefNodes[i].GetStringValue("variant", "Default");
                coreDefs = ROTModelData.getModelDefinitionLayouts(coreDefNodes[i].GetStringValues("model"));
                coreDefList.AddUniqueRange(coreDefs);
                ModelDefinitionVariantSet mdvs = getVariantSet(variantName);
                mdvs.addModels(coreDefs);
            }
            coreDefs = coreDefList.ToArray();

            //model defs - brought here so we can capture the array rather than the config node+method call
            ModelDefinitionLayoutOptions[] noseDefs = ROTModelData.getModelDefinitions(node.GetNodes("NOSE"));
            ModelDefinitionLayoutOptions[] mountDefs = ROTModelData.getModelDefinitions(node.GetNodes("MOUNT"));

            noseModule = new ROTModelModule<ModuleROTank>(part, this, getRootTransform("ModularPart-NOSE"), ModelOrientation.TOP, nameof(currentNose), null, nameof(currentNoseTexture), nameof(noseModulePersistentData));
            noseModule.name = "ModuleROTank-Nose";
            noseModule.getSymmetryModule = m => m.noseModule;
            noseModule.getValidOptions = () => noseDefs;

            coreModule = new ROTModelModule<ModuleROTank>(part, this, getRootTransform("ModularPart-CORE"), ModelOrientation.CENTRAL, nameof(currentCore), null, nameof(currentCoreTexture), nameof(coreModulePersistentData));
            coreModule.name = "ModuleROTank-Core";
            coreModule.getSymmetryModule = m => m.coreModule;
            coreModule.getValidOptions = () => getVariantSet(currentVariant).definitions;

            mountModule = new ROTModelModule<ModuleROTank>(part, this, getRootTransform("ModularPart-MOUNT"), ModelOrientation.BOTTOM, nameof(currentMount), null, nameof(currentMountTexture), nameof(mountModulePersistentData));
            mountModule.name = "ModuleROTank-Mount";
            mountModule.getSymmetryModule = m => m.mountModule;
            mountModule.getValidOptions = () => mountDefs;

            noseModule.massScalar = massScalingPower;
            coreModule.massScalar = massScalingPower;
            mountModule.massScalar = massScalingPower;

            noseModule.volumeScalar = volumeScalingPower;
            coreModule.volumeScalar = volumeScalingPower;
            mountModule.volumeScalar = volumeScalingPower;

            //set up the model lists and load the currently selected model
            noseModule.setupModelList(noseDefs);
            coreModule.setupModelList(coreDefs);
            mountModule.setupModelList(mountDefs);
            coreModule.setupModel();
            noseModule.setupModel();
            mountModule.setupModel();

            updateModulePositions();
            updateMassAndCost();
            updateAttachNodes(false);
            updateAvailableVariants();
            ROTStockInterop.updatePartHighlighting(part);
        }
        
        /// <summary>
        /// Initialize the UI controls, including default values, and specifying delegates for their 'onClick' methods.<para/>
        /// All UI based interaction code will be defined/run through these delegates.
        /// </summary>
        private void initializeUI()
        {
            Action<ModuleROTank> modelChangedAction = (m) =>
            {
                m.updateModulePositions();
                m.updateMassAndCost();
                m.updateAttachNodes(true);
                m.updateFairing(true);
                m.updateAvailableVariants();
                m.updateDragCubes();
                ROTModInterop.updateResourceVolume(m.part);
            };

            //set up the core variant UI control
            string[] variantNames = ROTUtils.getNames(variantSets.Values, m => m.variantName);
            this.updateUIChooseOptionControl(nameof(currentVariant), variantNames, variantNames, true, currentVariant);
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
                this.actionWithSymmetry(m => 
                {
                    m.currentVariant = currentVariant;
                    m.coreModule.modelSelected(newCoreDef.definition.name);
                    modelChangedAction(m);
                });
            };

            Fields[nameof(currentDiameter)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                this.actionWithSymmetry(m =>
                {
                    if (m != this) { m.currentDiameter = this.currentDiameter; }
                    modelChangedAction(m);
                    m.prevDiameter = m.currentDiameter;
                });
                ROTStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentVScale)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                this.actionWithSymmetry(m =>
                {
                    if (m != this) { m.currentVScale = this.currentVScale; }
                    modelChangedAction(m);
                });
                ROTStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentNose)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                noseModule.modelSelected(a, b);
                this.actionWithSymmetry(modelChangedAction);
                ROTStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentCore)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                coreModule.modelSelected(a, b);
                this.actionWithSymmetry(modelChangedAction);
                ROTStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentMount)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                mountModule.modelSelected(a, b);
                this.actionWithSymmetry(modelChangedAction);
                ROTStockInterop.fireEditorUpdate();
            };

            //------------------MODEL DIAMETER SWITCH UI INIT---------------------//
            if (maxDiameter == minDiameter)
            {
                Fields[nameof(currentDiameter)].guiActiveEditor = false;
            }
            else
            {
                this.updateUIFloatEditControl(nameof(currentDiameter), minDiameter, maxDiameter, diameterLargeStep, diameterSmallStep, diameterSlideStep, true, currentDiameter);
            }
            Fields[nameof(currentVScale)].guiActiveEditor = enableVScale;

            //------------------MODULE TEXTURE SWITCH UI INIT---------------------//
            Fields[nameof(currentNoseTexture)].uiControlEditor.onFieldChanged = noseModule.textureSetSelected;
            Fields[nameof(currentCoreTexture)].uiControlEditor.onFieldChanged = coreModule.textureSetSelected;
            Fields[nameof(currentMountTexture)].uiControlEditor.onFieldChanged = mountModule.textureSetSelected;

            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }
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

            //next, set nose scale values
            noseModule.setDiameterFromBelow(coreModule.moduleUpperDiameter, currentVScale);

            //finally, set mount scale values
            mountModule.setDiameterFromAbove(coreModule.moduleLowerDiameter, currentVScale);

            //total height of the part is determined by the sum of the heights of the modules at their current scale
            float totalHeight = noseModule.moduleHeight;
            totalHeight += coreModule.moduleHeight;
            totalHeight += mountModule.moduleHeight;

            //position of each module is set such that the vertical center of the models is at part origin/COM
            float pos = totalHeight * 0.5f;//abs top of model
            pos -= noseModule.moduleHeight;//bottom of nose model
            noseModule.setPosition(pos);
            pos -= coreModule.moduleHeight * 0.5f;//center of 'core' model
            coreModule.setPosition(pos);
            pos -= coreModule.moduleHeight * 0.5f;//bottom of 'core' model
            mountModule.setPosition(pos);

            //update actual model positions and scales
            noseModule.updateModelMeshes();
            coreModule.updateModelMeshes();
            mountModule.updateModelMeshes();
        }

        /// <summary>
        /// Update the cached modifiedMass and modifiedCost field values.  Used with stock cost/mass modifier interface.<para/>
        /// Optionally includes adapter mass/cost if enabled in config.
        /// </summary>
        private void updateMassAndCost()
        {
            modifiedMass = coreModule.moduleMass;
            if (useAdapterMass)
            {
                modifiedMass += noseModule.moduleMass;
                modifiedMass += mountModule.moduleMass;
            }

            modifiedCost = coreModule.moduleCost;
            if (useAdapterCost)
            {
                modifiedCost += noseModule.moduleCost;
                modifiedCost += mountModule.moduleCost;
            }

            float noseMaxDiam, mountMaxDiam = 0.0f;
            noseMaxDiam = Math.Max(noseModule.moduleLowerDiameter, noseModule.moduleUpperDiameter);
            ROTLog.debug("currentMount: " + currentMount);
            if (currentMount.Contains("Mount"))
            {
                ROTLog.debug("currentMount: " + currentMount);
                mountMaxDiam = mountModule.moduleUpperDiameter;
            }
            else
                mountMaxDiam = Math.Max(mountModule.moduleLowerDiameter, mountModule.moduleUpperDiameter);

            totalTankLength = getTotalHeight();
            ROTLog.debug("The Total Tank Length is: " + totalTankLength);
            largestDiameter = Math.Max(noseMaxDiam, mountMaxDiam);
        }

        /// <summary>
        /// Update the attach nodes for the current model-module configuration. 
        /// The 'nose' module is responsible for updating of upper attach nodes, while the 'mount' module is responsible for lower attach nodes.
        /// Also includes updating of 'interstage' nose/mount attach nodes.
        /// Also includes updating of surface-attach node position.
        /// Also includes updating of any parts that are surface attached to this part.
        /// </summary>
        /// <param name="userInput"></param>
        public void updateAttachNodes(bool userInput)
        {
            //update the standard top and bottom attach nodes, using the node position(s) defined in the nose and mount modules
            noseModule.updateAttachNodeTop("top", userInput);
            mountModule.updateAttachNodeBottom("bottom", userInput);

            //update the model-module specific attach nodes, using the per-module node definitions from the part
            noseModule.updateAttachNodeBody(noseNodeNames, userInput);
            coreModule.updateAttachNodeBody(coreNodeNames, userInput);
            mountModule.updateAttachNodeBody(mountNodeNames, userInput);

            //update the nose interstage node, using the node position as specified by the nose module's fairing offset parameter
            // ModelModule<ModuleROTank> nodeModule = getUpperFairingModelModule();
            // Vector3 pos = new Vector3(0, nodeModule.fairingBottom, 0);
            float theCoreNode = 0.0f;
            theCoreNode = coreModule.moduleHeight * 0.5f;
            Vector3 pos = new Vector3(0, theCoreNode, 0);
            ModuleROTSelectableNodes.updateNodePosition(part, noseInterstageNode, pos);
            AttachNode noseInterstage = part.FindAttachNode(noseInterstageNode);
            if (noseInterstage != null)
            {
                ROTAttachNodeUtils.updateAttachNodePosition(part, noseInterstage, pos, Vector3.up, userInput);
            }

            //update the nose interstage node, using the node position as specified by the nose module's fairing offset parameter
            // nodeModule = getLowerFairingModelModule();
            theCoreNode = -theCoreNode;
            pos = new Vector3(0, theCoreNode, 0);
            ModuleROTSelectableNodes.updateNodePosition(part, mountInterstageNode, pos);
            AttachNode mountInterstage = part.FindAttachNode(mountInterstageNode);
            if (mountInterstage != null)
            {
                ROTAttachNodeUtils.updateAttachNodePosition(part, mountInterstage, pos, Vector3.down, userInput);
            }

            //update surface attach node position, part position, and any surface attached children
            //TODO -- how to determine how far to offset/move surface attached children?
            AttachNode surfaceNode = part.srfAttachNode;
            if (surfaceNode != null)
            {
                coreModule.updateSurfaceAttachNode(surfaceNode, prevDiameter, userInput);
            }
        }
        
        /// <summary>
        /// Update the current fairing modules (top, centra, and bottom) for the current model-module configuration (diameters, positions).
        /// </summary>
        /// <param name="userInput"></param>
        private void updateFairing(bool userInput)
        {
            ModuleROTNodeFairing[] modules = part.GetComponents<ModuleROTNodeFairing>();
            if (centralFairingIndex >= 0 && centralFairingIndex < modules.Length)
            {
                bool enabled = coreModule.fairingEnabled;
                ModuleROTNodeFairing coreFairing = modules[centralFairingIndex];
                float top = coreModule.fairingTop;
                float bot = coreModule.fairingBottom;
                ROTFairingUpdateData data = new ROTFairingUpdateData();
                data.setTopY(top);
                data.setTopRadius(currentDiameter * 0.5f);
                data.setBottomY(bot);
                data.setBottomRadius(currentDiameter * 0.5f);
                data.setEnable(enabled);
                coreFairing.updateExternal(data);
            }
            if (topFairingIndex >= 0 && topFairingIndex < modules.Length)
            {
                ROTModelModule<ModuleROTank> moduleForUpperFiaring = getUpperFairingModelModule();
                bool enabled = moduleForUpperFiaring.fairingEnabled;
                ModuleROTNodeFairing topFairing = modules[topFairingIndex];
                float topFairingBottomY = moduleForUpperFiaring.fairingBottom;
                ROTFairingUpdateData data = new ROTFairingUpdateData();
                data.setTopY(getPartTopY());
                data.setBottomY(topFairingBottomY);
                data.setBottomRadius(currentDiameter * 0.5f);
                data.setEnable(enabled);
                if (userInput) { data.setTopRadius(currentDiameter * 0.5f); }
                topFairing.updateExternal(data);
            }
            if (bottomFairingIndex >= 0 && bottomFairingIndex < modules.Length)
            {
                ROTModelModule<ModuleROTank> moduleForLowerFairing = getLowerFairingModelModule();
                bool enabled = moduleForLowerFairing.fairingEnabled;
                ModuleROTNodeFairing bottomFairing = modules[bottomFairingIndex];
                float bottomFairingTopY = moduleForLowerFairing.fairingTop;
                ROTFairingUpdateData data = new ROTFairingUpdateData();
                data.setTopRadius(currentDiameter * 0.5f);
                data.setTopY(bottomFairingTopY);
                data.setEnable(enabled);
                if (userInput) { data.setBottomRadius(currentDiameter * 0.5f); }
                bottomFairing.updateExternal(data);
            }
        }

        /// <summary>
        /// Return the total height of this part in its current configuration.  This will be the distance from the bottom attach node to the top attach node, and may not include any 'extra' structure. TOOLING
        /// </summary>
        /// <returns></returns>
        private float getTotalHeight()
        {
            float totalHeight = noseModule.moduleHeight;
            totalHeight += mountModule.moduleHeight;
            ROTLog.debug("currentCore: " + currentCore);
            if (currentCore.Contains("Booster"))
            {
                ROTLog.debug("currentCore: " + currentCore);
                totalHeight += coreModule.moduleActualHeight;
            }
            else
                totalHeight += coreModule.moduleHeight;
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
        /// Return the ModelModule slot responsible for upper attach point of lower fairing module
        /// </summary>
        /// <returns></returns>
        private ROTModelModule<ModuleROTank> getLowerFairingModelModule()
        {
            float coreBaseDiam = coreModule.moduleDiameter;
            if (coreModule.moduleLowerDiameter < coreBaseDiam) { return coreModule; }
            return mountModule;
        }

        /// <summary>
        /// Return the ModelModule slot responsible for lower attach point of the upper fairing module
        /// </summary>
        /// <returns></returns>
        private ROTModelModule<ModuleROTank> getUpperFairingModelModule()
        {
            float coreBaseDiam = coreModule.moduleDiameter;
            if (coreModule.moduleUpperDiameter < coreBaseDiam) { return coreModule; }
            return noseModule;
        }

        /// <summary>
        /// Update the UI visibility for the currently available selections.<para/>
        /// Will hide/remove UI fields for slots with only a single option (models, textures, layouts).
        /// </summary>
        private void updateAvailableVariants()
        {
            noseModule.updateSelections();
            coreModule.updateSelections();
            mountModule.updateSelections();
        }

        /// <summary>
        /// Calls the generic ROT procedural drag-cube updating routines.  Will update the drag cubes for whatever the current model state is.
        /// </summary>
        private void updateDragCubes()
        {
            ROTModInterop.onPartGeometryUpdate(part, true);
        }
        
        /// <summary>
        /// Return the root transform for the specified name.  If does not exist, will create it and parent it to the parts' 'model' transform.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="recreate"></param>
        /// <returns></returns>
        private Transform getRootTransform(string name)
        {
            Transform root = part.transform.FindRecursive(name);
            if (root != null)
            {
                GameObject.DestroyImmediate(root.gameObject);
                root = null;
            }
            root = new GameObject(name).transform;
            root.NestToParent(part.transform.FindRecursive("model"));
            return root;
        }

        /// <summary>
        /// Return the model-module corresponding to the input slot name.  Valid slot names are: NOSE,UPPER,CORE,LOWER,MOUNT
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ROTModelModule<ModuleROTank> getModuleByName(string name)
        {
            switch (name)
            {
                case "NOSE":
                    return noseModule;
                case "CORE":
                    return coreModule;
                case "MOUNT":
                    return mountModule;
                case "NONE":
                    return null;
                default:
                    return null;
            }
        }

        #endregion ENDREGION - Custom Update Methods

    }

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
