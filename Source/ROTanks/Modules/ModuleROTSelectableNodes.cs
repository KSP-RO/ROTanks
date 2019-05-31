using System;
using UnityEngine;

namespace ROTanks
{
    public class ModuleROTSelectableNodes : PartModule
    {
        [KSPField]
        public String nodeName = "top";

        [KSPField]
        public bool startsEnabled = true;

        [KSPField(isPersistant = true)]
        public bool currentlyEnabled = false;

        [KSPField(isPersistant = true)]
        public bool initialized = false;

        [KSPField]
        public Vector3 nodeDefaultPosition = Vector3.zero;

        [KSPField]
        public Vector3 nodeDefaultOrientation = Vector3.down;

        protected BaseEvent tEvent;

        [KSPEvent(guiName = "Toggle Node", guiActiveEditor = true)]
        public void toggleNodeEvent()
        {
            toggleNode();
            if (currentlyEnabled)
            {
                tEvent.guiName = nodeName + ": Enabled";
                return;
            }
            else
                tEvent.guiName = nodeName + ": Disabled";
        }

        public override void OnAwake()
        {
            base.OnAwake();
            tEvent = Events["toggleNodeEvent"];
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            tEvent = Events["toggleNodeEvent"];

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                if (!initialized)
                {
                    currentlyEnabled = startsEnabled;
                    initialized = true;
                    AttachNode node = part.FindAttachNode(nodeName);
                    if (currentlyEnabled && node == null)
                    {
                        ROTAttachNodeUtils.createAttachNode(part, nodeName, nodeDefaultPosition, nodeDefaultOrientation, 2);
                    }
                    else if (!currentlyEnabled && node != null && node.attachedPart == null)
                    {
                        ROTAttachNodeUtils.destroyAttachNode(part, node);
                    }
                    else if (!currentlyEnabled && node != null && node.attachedPart != null)//error, should never occur if things were handled properly
                    {
                        currentlyEnabled = true;
                    }
                }
                else
                {
                    AttachNode node = part.FindAttachNode(nodeName);
                    if (currentlyEnabled && node == null)
                    {
                        currentlyEnabled = true;
                        ROTAttachNodeUtils.createAttachNode(part, nodeName, nodeDefaultPosition, nodeDefaultOrientation, 2);
                    }
                    else if (!currentlyEnabled && node != null && node.attachedPart == null)
                    {
                        currentlyEnabled = false;
                        ROTAttachNodeUtils.destroyAttachNode(part, node);
                    }
                }
            }

            tEvent.guiName = currentlyEnabled ? nodeName + ": Enabled" : nodeName + ": Disabled";
        }

        public void toggleNode()
        {
            AttachNode node = part.FindAttachNode(nodeName);
            ROTLog.debug("toggleNode() node: " + node);
            if (node == null)
            {
                currentlyEnabled = true;
                ROTAttachNodeUtils.createAttachNode(part, nodeName, nodeDefaultPosition, nodeDefaultOrientation, 2);
            }
            else if (node.attachedPart == null)
            {
                currentlyEnabled = false;
                ROTAttachNodeUtils.destroyAttachNode(part, node);
            }
        }

        public static void updateNodePosition(Part part, String nodeName, Vector3 pos)
        {
            ModuleROTSelectableNodes[] modules = part.GetComponents<ModuleROTSelectableNodes>();
            int len = modules.Length;
            for (int i = 0; i < len; i++)
            {
                if (modules[i].nodeName == nodeName)
                {
                    modules[i].nodeDefaultPosition = pos;
                }
            }
        }
    }
}

