﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;


namespace HabUtils
{
    public enum DeployState
    {
        Retracted, Deploying, Deployed, Retracting
    }

    public class ModuleDeployableInventory : PartModule, IPartMassModifier, IMultipleDragCube
    {
        // Name of the deploy Animation
        [KSPField(isPersistant = false)]
        public string DeployAnimationName = "";

        // Speed of the deploy animation
        [KSPField(isPersistant = false)]
        public float AnimationSpeed = 1.0f;

        // Layer of the deploy animation
        [KSPField(isPersistant = false)]
        public int AnimationLayer = 1;

        // Crew capacity when deployd
        //[KSPField(isPersistant = false)]
        //public int RetractedCrewCapacity = 0;

        // Crew capacity when deployd
        //[KSPField(isPersistant = false)]
        //public int DeployedCrewCapacity = 2;

        // Crew capacity when deployd
        [KSPField(isPersistant = false)]
        public float DeployedInventoryCap = 1000;

        // Number of inventory slots when deployd
        [KSPField(isPersistant = false)]
        public int DeployedInventorySlots = 6;

        // The space this part occupies when packed inside an inventory container.If this is set to < 0 then the part can be manipulated in EVA construction mode but cannot be placed inside inventory containers.
        public float packedVolume = 0;
        // The tooltip for inventory cargo parts, shows up when mouse is over a part icon during flight.
        public string inventoryTooltip = "tooltip";
        // The number of parts of the same kind and variant that can be stacked in a single inventory slot. Defaults to 1, so leave it out if you DON’T want the part to be stackable.
        public int stackableQuantity = 0;



        // Is this a single-use module?
        [KSPField(isPersistant = true)]
        public bool Retractable = true;

        // The resouce required to deploy
        [KSPField(isPersistant = false)]
        public string DeployResource = "";

        // The amount required
        [KSPField(isPersistant = false)]
        public float DeployResourceAmount = 0f;

        // The number of crew needed to deploy
        [KSPField(isPersistant = false)]
        public int CrewToDeploy = 0;

        // The skill the crew needs
        [KSPField(isPersistant = false)]
        public string CrewSkillNeeded = "";

        // The skill the crew needs, localized
        [KSPField(isPersistant = false)]
        public string CrewSkillNeededName = "Engineer";

        // Is the module deployed
        [KSPField(isPersistant = true)]
        public bool Deployed = false;

        // Name of the deploy action
        [KSPField(isPersistant = false)]
        public string DeployActionName = "";

        // Name of the retract action
        [KSPField(isPersistant = false)]
        public string RetractActionName = "";

        // Name of the toggle action
        [KSPField(isPersistant = false)]
        public string ToggleActionName = "";

        // Name of the toggle action
        [KSPField(isPersistant = false)]
        public float DeployedMassModifier = 0.0f;

        /// GUI Fields
        // Current status of deploy
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Status")]
        public string DeployStatus = "N/A";

        // GUI Events
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Deploy", active = true)]
        public void Deploy()
        {
            TryDeploy();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Retract", active = false)]
        public void Retract()
        {
            TryRetract();
        }

        /// GUI Actions
        // ACTIONS
        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam param) { Deploy(); }

        [KSPAction("Retract")]
        public void RetractAction(KSPActionParam param) { Retract(); }

        [KSPAction("Toggle Deploy")]
        public void ToggleAction(KSPActionParam param)
        {
            if (Deployed)
                Retract();
            else
                Deploy();
        }

        /// protected
        protected float vabSpeedScale = 10f;
        protected DeployState deployState;
        protected AnimationState deployAnimation;
        protected List<BaseConverter> converters;


        public override string GetInfo()
        {
            string baseInfo = Localizer.Format("<<1>>", DeployedInventoryCap.ToString("F0"));
            if (!Retractable)
                baseInfo += "\n\n" + Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_PartInfo_NoRetract");
            if (DeployResource != "")
            {
                PartResourceDefinition defn = PartResourceLibrary.Instance.GetDefinition(DeployResource);
                baseInfo += "\n\n" + Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_PartInfo_Resources", defn.displayName, DeployResourceAmount.ToString("F2"));
            }
            if (CrewToDeploy > 0)
            {
                if (CrewSkillNeeded == "")
                {
                    baseInfo += "\n\n" + Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_PartInfo_NeedsCrewUnskilled", CrewToDeploy.ToString("F0"));
                }
                else
                {
                    baseInfo += "\n\n" + Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_PartInfo_NeedsCrew", CrewToDeploy.ToString("F0"), Localizer.Format(CrewSkillNeededName));
                }
            }

            return baseInfo;
        }
        public string GetModuleTitle()
        {
            return "Expandable Habitat";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_ModuleTitle");
        }

        public float GetModuleMass(float baseMass, ModifierStagingSituation situation)
        {
            if (Deployed)
                return DeployedMassModifier;
            else
                return 0f;
        }
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public override void OnLoad(ConfigNode node)
        {

            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {

            }
        }
        public virtual void Start()
        {
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                SetupAnimation();
                SetupUI();
                SetupState();
            }
        }

        public virtual void Update()
        {
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                HandleUI();
            }
        }

        public virtual void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                if (DeployAnimationName != "")
                {
                    HandleAnimation();
                    HandleConverters();
                }
                EvaluateAnimation();
            }
        }

        /// Setup the animation
        protected void SetupAnimation()
        {
            if (DeployAnimationName != "")
            {
                deployAnimation = Utils.SetUpAnimation(DeployAnimationName, this.part, AnimationLayer);
                if (deployAnimation == null)
                    Utils.LogError("[ModuleDeployableHabitat]: Could not find animation");
                else
                {


                }
            }
        }

        /// Do localization of UI components
        protected virtual void SetupUI()
        {
            Events["Deploy"].guiName = Localizer.Format(DeployActionName);
            Events["Retract"].guiName = Localizer.Format(RetractActionName);
            Actions["DeployAction"].guiName = Localizer.Format(DeployActionName);
            Actions["RetractAction"].guiName = Localizer.Format(RetractActionName);
            Actions["ToggleAction"].guiName = Localizer.Format(ToggleActionName);
        }

        /// Sets up the first load of the module
        protected void SetupState()
        {
            GetConverters();
            AddRemoveCargoInventory(Deployed);
            if (Deployed)
            {
                deployState = DeployState.Deployed;
                if (DeployAnimationName != "")
                    deployAnimation.normalizedTime = 0.0f;
                CreateIVA();
            }
            else
            {
                deployState = DeployState.Retracted;
                if (DeployAnimationName != "")
                    deployAnimation.normalizedTime = 1.0f;
                DestroyIVA();
                DisableConverters();
            }
            RefreshPartData();
        }

        protected void GetConverters()
        {
            converters = part.GetComponents<BaseConverter>().ToList();
        }

        protected virtual void HandleConverters()
        {
            if (converters != null)
            {
                if (deployState != DeployState.Deployed)
                    DisableConverters();
            }
        }
        /// Handle updating the UI
        protected virtual void HandleUI()
        {
            switch (deployState)
            {
                case DeployState.Retracted:
                    DeployStatus = "Retracted";
                    Events["Deploy"].active = true;
                    Events["Retract"].active = false;
                    break;
                case DeployState.Deployed:
                    DeployStatus = "Deployed";
                    Events["Deploy"].active = false;
                    if (HighLogic.LoadedSceneIsFlight && !Retractable)
                        Events["Retract"].active = false;
                    else
                        Events["Retract"].active = true;
                    break;
                case DeployState.Deploying:
                    DeployStatus = "Deploying";
                    Events["Deploy"].active = false;
                    Events["Retract"].active = true;
                    break;
                case DeployState.Retracting:
                    DeployStatus = "Retracting";
                    Events["Deploy"].active = true;
                    Events["Retract"].active = false;
                    break;
            }
        }

        /// Handle updating the animation
        protected void HandleAnimation()
        {

            float realSpeed = AnimationSpeed;
            if (HighLogic.LoadedSceneIsEditor)
                realSpeed = realSpeed * 10f;

            switch (deployState)
            {
                case DeployState.Retracted:
                    break;
                case DeployState.Deployed:
                    break;
                case DeployState.Deploying:
                    deployAnimation.normalizedTime = Mathf.MoveTowards(deployAnimation.normalizedTime, 0.0f, realSpeed * TimeWarp.fixedDeltaTime);
                    SetDragCubeState(deployAnimation.normalizedTime);
                    break;
                case DeployState.Retracting:
                    deployAnimation.normalizedTime = Mathf.MoveTowards(deployAnimation.normalizedTime, 1.0f, realSpeed * TimeWarp.fixedDeltaTime);
                    SetDragCubeState(deployAnimation.normalizedTime);
                    break;
            }

        }

        protected void EvaluateAnimation()
        {
            if (DeployAnimationName != "")
            {
                if (deployState == DeployState.Deploying && deployAnimation.normalizedTime <= 0.0)
                {
                    FinishDeploy();


                }
                if (deployState == DeployState.Retracting && deployAnimation.normalizedTime >= 1.0)
                {
                    FinishRetract();

                }
            }
            else
            {
                if (deployState == DeployState.Deploying)
                {
                    FinishDeploy();


                }
                if (deployState == DeployState.Retracting)
                {
                    FinishRetract();

                }
            }
        }

        /// Deploys the part if it passes the check
        public void TryDeploy()
        {
            if (CanDeploy())
            {
                StartDeploy();
            }
        }

        /// Retracts the part if it passes the check
        public void TryRetract()
        {
            if (CanRetract())
            {
                StartRetract();
            }
        }

        public bool IsMultipleCubesActive
        {
            get { return true; }
        }

        /// Called to start deflation
        protected virtual void StartRetract()
        {
            Utils.Log("[ModuleDeployableHabitat]: Retract Started");
            deployState = DeployState.Retracting;
            AddRemoveCargoInventory(false);
            DisableConverters();
            DestroyIVA();
            RefreshPartData();

        }
        /// Called to start deploy
        protected virtual void StartDeploy()
        {
            Utils.Log("[ModuleDeployableHabitat]: Deploy Started");
            deployState = DeployState.Deploying;
            if (HighLogic.LoadedSceneIsFlight && DeployResource != "")
            {
                part.RequestResource(DeployResource, (double)DeployResourceAmount);
            }
        }

        /// Execute actions on deflation completion
        protected virtual void FinishRetract()
        {
            Utils.Log("[ModuleDeployableHabitat]: Retract Finished");
            deployState = DeployState.Retracted;
            SetDragCubeState(1.0f);
            DisableConverters();
            Deployed = false;
            RefreshPartData();
        }

        /// Execute actions on deploy completion
        protected virtual void FinishDeploy()
        {
            Utils.Log("[ModuleDeployableHabitat]: Deploy Finished");

            deployState = DeployState.Deployed;
            Deployed = true;
            SetDragCubeState(0.0f);

            AddRemoveCargoInventory(Deployed);
            CreateIVA();
            RefreshPartData();
        }

        protected void RefreshPartData()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartTweaked, part);
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                //GameEvents.onVesselWasModified.Fire(this.vessel);
                part.CheckTransferDialog();
            }
            MonoUtilities.RefreshContextWindows(part);
        }


        // From @Tanihwa
        void ShowVolume(Part part)
        {
            var mfList = part.FindModelComponents<MeshFilter>();
            double vol = 0;
            foreach (var mf in mfList)
            {
                Debug.Log("Part: " + part.partName + ", mesh: " + mf.name);
                Mesh mesh = mf.sharedMesh;
                Vector3[] verts = mesh.vertices;
                for (int sm = 0; sm < mesh.subMeshCount; sm++)
                {
                    int[] tris = mesh.GetTriangles(sm);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        Vector3d a = verts[tris[i]];
                        Vector3d b = verts[tris[i + 1]];
                        Vector3d c = verts[tris[i + 2]];
                        vol += Vector3d.Dot(c, Vector3d.Cross(a, b)) / 6;
                    }
                }
            }

            vol *= 1000;

            Bounds bounds = default(Bounds);

            foreach (Bounds rendererBound in PartGeometryUtil.GetRendererBounds(part))
                bounds.Encapsulate(rendererBound);

            float boundvol = (float)(bounds.size.x * bounds.size.y * bounds.size.z) * 1000f;

            Debug.Log("Part: " + part.partName + " " + 
                bounds.size.x.ToString("F2") + " " + bounds.size.y.ToString("F2") + " " + bounds.size.z.ToString("F2") +
                ", boundvol: " + boundvol.ToString("F2") +
                ", Tanihwavol: " + vol.ToString("F2"));
        }

        /// Set the crew capacity of the part
        /// TODO: Implement me
        protected void AddRemoveCargoInventory(bool Deployed)
        {
            bool flag = Deployed;

            foreach (var m in part.Modules)
                Debug.Log(m.moduleName);

            Debug.Log("AddRemoveCargoInventoryBefore: Deployed: " + flag);
            Debug.Log("ModuleCargoPart: " + part.Modules.OfType<ModuleCargoPart>().Count() );
            Debug.Log("ModuleInventoryPart: " + part.Modules.OfType<ModuleInventoryPart>().Count());

            var cargos = part.Modules.OfType<ModuleCargoPart>();
            var invs = part.Modules.OfType<ModuleInventoryPart>();

            if (flag)
            {
                if (cargos.Count() == 1)
                {
                    var cargo = cargos.Single();

                    packedVolume = cargo.packedVolume;
                    stackableQuantity = cargo.stackableQuantity;
                    inventoryTooltip = cargo.inventoryTooltip;
                    part.RemoveModule(cargo);
                }

                if (invs.Count() == 0)
                {
                    var inv = (ModuleInventoryPart)part.AddModule("ModuleInventoryPart", true);
                    inv.OnStart(part.GetModuleStartState());
                    inv.packedVolumeLimit = DeployedInventoryCap;
                    inv.InventorySlots = DeployedInventorySlots;
                }
            }
            else
            {
                if (invs.Count() == 1)
                {
                    var inv = invs.Single();
                    part.RemoveModule(inv);
                }

                if (cargos.Count() == 0)
                {
                    var cargo = (ModuleCargoPart)part.AddModule("ModuleCargoPart", true);
                    cargo.packedVolume = packedVolume;
                    cargo.stackableQuantity = stackableQuantity;
                    cargo.inventoryTooltip = inventoryTooltip;
                }
            }

            Debug.Log("AddRemoveCargoInventoryAfter: Deployed: " + flag);
            Debug.Log("ModuleCargoPart: " + part.Modules.OfType<ModuleCargoPart>().Count());
            Debug.Log("ModuleInventoryPart: " + part.Modules.OfType<ModuleInventoryPart>().Count());
            ShowVolume(part);

        }

        /// Creates the IVA space
        /// TODO: Implement me
        protected void CreateIVA()
        {
            if (HighLogic.LoadedSceneIsFlight)
                this.part.SpawnIVA();
        }

        /// Destroys the IVA space
        /// TODO: Implement me
        protected void DestroyIVA()
        {
            if (HighLogic.LoadedSceneIsFlight)
                this.part.DespawnIVA();
        }

        /// Checks to see if we can deploy
        protected bool CanDeploy()
        {
            // Cannot deploy if enough engineers are not present
            if (HighLogic.LoadedSceneIsFlight && CrewToDeploy > 0)
            {
                List<ProtoCrewMember> crew = part.vessel.GetVesselCrew();

                int numCrew = 0;
                if (CrewSkillNeeded != "")
                {
                    foreach (ProtoCrewMember crewman in crew)
                    {

                        if (crewman.experienceTrait.TypeName == CrewSkillNeeded)
                        {
                            numCrew++;
                        }
                    }
                }
                else
                {
                    numCrew = crew.Count;
                }

                if (numCrew < CrewToDeploy)
                {
                    string msg;
                    if (CrewSkillNeeded == "")
                    {
                        msg = Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_Message_CantDeployCrewUnskilled",
                                    part.partInfo.title, CrewToDeploy);
                    }
                    else
                    {
                        msg = Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_Message_CantDeployCrew",
                                    part.partInfo.title, CrewToDeploy, Localizer.Format(CrewSkillNeededName));
                    }
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }

            }

            // Cannot deploy if deploy resource is not present
            if (HighLogic.LoadedSceneIsFlight && DeployResource != "")
            {
                PartResourceDefinition defn = PartResourceLibrary.Instance.GetDefinition(DeployResource);
                double res = 0d;
                double outRes = 0d;
                part.GetConnectedResourceTotals(defn.id, out res, out outRes, true);
                res = Math.Round(res);
                if (res < DeployResourceAmount)
                {
                    var msg = Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_Message_CantDeployResources",
                                part.partInfo.title, DeployResourceAmount.ToString("F2"), defn.displayName);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
            }
            if (deployState == DeployState.Retracted || deployState == DeployState.Retracting)
                return true;

            return false;
        }


        /// Checks to see if we can deflate or not
        protected bool CanRetract()
        {
            // Cannot retract if that is disabled!
            if (HighLogic.LoadedSceneIsFlight && !Retractable)
                return false;


            // Cannot retract if crew are present
            if (part.protoModuleCrew.Count > 0)
            {
                var msg = Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_Message_CantRetractCrew",
                            part.partInfo.title);
                ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            if (deployState == DeployState.Deployed || deployState == DeployState.Deploying)
                return true;

            return false;
        }

        // So very confusing
        public bool UsesProceduralDragCubes()
        {
            return false;
        }

        // Let's call the cubes A and B
        public string[] GetDragCubeNames()
        {
            return new String[] { "A", "B" };
        }

        public void AssumeDragCubePosition(string name)
        {
            Animation[] anims = part.FindModelAnimators(DeployAnimationName);

            anims[0][DeployAnimationName].speed = 0f;
            anims[0][DeployAnimationName].enabled = true;
            anims[0][DeployAnimationName].weight = 1f;
            switch (name)
            {
                case "A":
                    anims[0][DeployAnimationName].normalizedTime = 1f;
                    break;
                case "B":
                    anims[0][DeployAnimationName].normalizedTime = 0f;
                    break;
            }
        }

        private void SetDragCubeState(float progress)
        {
            part.DragCubes.SetCubeWeight("A", progress);
            part.DragCubes.SetCubeWeight("B", 1f - progress);
            if (part.DragCubes.Procedural)
            {
                part.DragCubes.ForceUpdate(true, true);
            }
        }

        protected void DisableConverters()
        {
            foreach (BaseConverter conv in converters)
            {
                if (conv.IsActivated)
                {
                    conv.DisableModule();
                }
            }
        }


    }
}