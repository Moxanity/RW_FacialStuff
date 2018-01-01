﻿namespace FacialStuff
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    using FacialStuff.Animator;
    using FacialStuff.DefOfs;
    using FacialStuff.Defs;
    using FacialStuff.Graphics;

    using JetBrains.Annotations;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public class CompBodyAnimator : ThingComp
    {
        #region Public Fields

        public bool AnimatorOpen;
        public BodyAnimDef bodyAnim;
        public BodyPartStats bodyStat;
        public float JitterMax = 0.35f;
        public PawnBodyGraphic PawnBodyGraphic;
        public WalkCycleDef WalkCycle = WalkCycleDefOf.Biped_Walk;

        #endregion Public Fields

        #region Private Fields

        private static FieldInfo infoJitterer;
        [NotNull]
        private List<Material> cachedNakedMatsBodyBase = new List<Material>();

        private int cachedNakedMatsBodyBaseHash = -1;
        private List<Material> cachedSkinMatsBodyBase = new List<Material>();
        private int cachedSkinMatsBodyBaseHash = -1;
        private bool initialized;
        private int lastRoomCheck;
        private Room theRoom;

        #endregion Private Fields

        #region Public Properties

        public BodyAnimator BodyAnimator { get; private set; }

        public bool HideShellLayer => this.InRoom && Controller.settings.HideShellWhileRoofed;

        public bool InPrivateRoom
        {
            get
            {
                if (!this.InRoom || this.Pawn.IsPrisoner)
                {
                    return false;
                }

                Room ownedRoom = this.Pawn.ownership?.OwnedRoom;
                if (ownedRoom != null)
                {
                    return ownedRoom == this.TheRoom;
                }

                return false;
            }
        }

        public bool InRoom
        {
            get
            {
                if (this.TheRoom != null)
                {
                    RoomGroup theRoomGroup = this.TheRoom.Group;
                    if (theRoomGroup != null && !theRoomGroup.UsesOutdoorTemperature)
                    {
                        // Pawn is indoors
                        return !this.Pawn.Drafted || !Controller.settings.IgnoreWhileDrafted;
                    }
                }

                return false;

                // return !room?.Group.UsesOutdoorTemperature == true && Controller.settings.IgnoreWhileDrafted || !this.pawn.Drafted;
            }
        }

        public JitterHandler Jitterer
            => GetHiddenValue(typeof(Pawn_DrawTracker), this.Pawn.Drawer, "jitterer", infoJitterer) as
                   JitterHandler;

        [NotNull]
        public Pawn Pawn => this.parent as Pawn;

        public List<PawnBodyDrawer> PawnBodyDrawers { get; private set; }

        public CompProperties_BodyAnimator Props
        {
            get
            {
                return (CompProperties_BodyAnimator)this.props;
            }
        }

        public bool HideHat => this.InRoom && Controller.settings.HideHatWhileRoofed;

        #endregion Public Properties

        #region Private Properties

        [CanBeNull]
        private Room TheRoom
        {
            get
            {
                if (this.Pawn.Dead)
                {
                    return null;
                }

                if (Find.TickManager.TicksGame < this.lastRoomCheck + 60f)
                {
                    return this.theRoom;
                }

                this.theRoom = this.Pawn.GetRoom();
                this.lastRoomCheck = Find.TickManager.TicksGame;

                return this.theRoom;
            }
        }

        #endregion Private Properties

        #region Public Methods

        public static object GetHiddenValue(Type type, object instance, string fieldName, [CanBeNull] FieldInfo info)
        {
            if (info == null)
            {
                info = type.GetField(fieldName, GenGeneric.BindingFlagsAll);
            }

            return info?.GetValue(instance);
        }

        public void ApplyBodyWobble(ref Vector3 rootLoc, ref Vector3 footPos,  ref Quaternion quat)
        {
            if (this.PawnBodyDrawers != null)
            {
                int i = 0;
                int count = this.PawnBodyDrawers.Count;
                while (i < count)
                {
                    this.PawnBodyDrawers[i].ApplyBodyWobble(ref rootLoc, ref footPos, ref quat);
                    i++;
                }
            }
        }

        // Verse.PawnGraphicSet
        public void ClearCache()
        {
            this.cachedSkinMatsBodyBaseHash = -1;
            this.cachedNakedMatsBodyBaseHash = -1;
        }

        // public override string CompInspectStringExtra()
        // {
        //     string extra = this.Pawn.DrawPos.ToString();
        //     return extra;
        // }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void DrawBody(Vector3 rootLoc, Quaternion quat, RotDrawMode bodyDrawType, [CanBeNull] PawnWoundDrawer woundDrawer, bool renderBody, bool portrait)
        {
            if (this.PawnBodyDrawers.NullOrEmpty())
            {
                return;
            }

            int i = 0;

            while (i < this.PawnBodyDrawers.Count)
            {
                this.PawnBodyDrawers[i].DrawBody(
                    woundDrawer,
                    rootLoc,
                    quat,
                    bodyDrawType,
                    renderBody,
                    portrait);
                i++;
            }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void DrawEquipment(Vector3 rootLoc, bool portrait)
        {
            if (!this.PawnBodyDrawers.NullOrEmpty())
            {
                int i = 0;
                int count = this.PawnBodyDrawers.Count;
                while (i < count)
                {
                    this.PawnBodyDrawers[i].DrawEquipment(rootLoc, portrait);
                    i++;
                }
            }
        }
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void DrawFeet(Vector3 rootLoc, bool portrait)
        {
            if (!this.PawnBodyDrawers.NullOrEmpty())
            {
                int i = 0;
                int count = this.PawnBodyDrawers.Count;
                while (i < count)
                {
                    this.PawnBodyDrawers[i].DrawFeet(rootLoc, portrait);
                    i++;
                }
            }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void DrawHands(Vector3 rootLoc, bool portrait, bool carrying)
        {
            if (!this.PawnBodyDrawers.NullOrEmpty())
            {
                int i = 0;
                int count = this.PawnBodyDrawers.Count;
                while (i < count)
                {
                    this.PawnBodyDrawers[i].DrawHands(rootLoc, portrait, carrying);
                    i++;
                }
            }
        }

        public void InitializePawnDrawer()
        {
            if (this.Props.drawers.Any())
            {
                this.PawnBodyDrawers = new List<PawnBodyDrawer>();
                for (int i = 0; i < this.Props.drawers.Count; i++)
                {
                    PawnBodyDrawer thingComp = (PawnBodyDrawer)Activator.CreateInstance(this.Props.drawers[i].GetType());
                    thingComp.CompAnimator = this;
                    thingComp.Pawn = this.Pawn;
                    this.PawnBodyDrawers.Add(thingComp);
                    thingComp.Initialize();
                }
            }
            else
            {
                this.PawnBodyDrawers = new List<PawnBodyDrawer>();

                PawnBodyDrawer thingComp = (PawnBodyDrawer)Activator.CreateInstance(typeof(HumanBipedDrawer));
                thingComp.CompAnimator = this;
                thingComp.Pawn = this.Pawn;
                this.PawnBodyDrawers.Add(thingComp);
                thingComp.Initialize();


            }
        }

        public List<Material> NakedMatsBodyBaseAt(Rot4 facing, RotDrawMode bodyCondition = RotDrawMode.Fresh)
        {
            int num = facing.AsInt + 1000 * (int)bodyCondition;
            if (num != this.cachedNakedMatsBodyBaseHash)
            {
                this.cachedNakedMatsBodyBase.Clear();
                this.cachedNakedMatsBodyBaseHash = num;
                PawnGraphicSet graphics = this.Pawn.Drawer.renderer.graphics;
                if (bodyCondition == RotDrawMode.Fresh)
                {
                    this.cachedNakedMatsBodyBase.Add(graphics.nakedGraphic.MatAt(facing));
                }
                else if (bodyCondition == RotDrawMode.Rotting || graphics.dessicatedGraphic == null)
                {
                    this.cachedNakedMatsBodyBase.Add(graphics.rottingGraphic.MatAt(facing));
                }
                else if (bodyCondition == RotDrawMode.Dessicated)
                {
                    this.cachedNakedMatsBodyBase.Add(graphics.dessicatedGraphic.MatAt(facing));
                }

                for (int i = 0; i < graphics.apparelGraphics.Count; i++)
                {
                    ApparelLayer lastLayer = graphics.apparelGraphics[i].sourceApparel.def.apparel.LastLayer;

                    if (this.Pawn.Dead)
                    {
                        if (lastLayer != ApparelLayer.Shell && lastLayer != ApparelLayer.Overhead)
                        {
                            this.cachedNakedMatsBodyBase.Add(graphics.apparelGraphics[i].graphic.MatAt(facing));
                        }
                    }
                }
            }

            return this.cachedNakedMatsBodyBase;
        }

        public override void PostDraw()
        {
            base.PostDraw();

            // Children & Pregnancy || Werewolves transformed
            if (this.Pawn.Map == null || !this.Pawn.Spawned || this.Pawn.Dead)
            {
                return;
            }

            if (Find.TickManager.Paused)
            {
                return;
            }

            if (this.Props.bipedWithHands)
            {
                this.BodyAnimator.AnimatorTick();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.lastRoomCheck, "lastRoomCheck");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            this.BodyAnimator = new BodyAnimator(this.Pawn, this);

            this.PawnBodyGraphic = new PawnBodyGraphic(this);

            BodyType bodyType = BodyType.Undefined;

            if (this.Pawn.story?.bodyType != null)
            {
                bodyType = this.Pawn.story.bodyType;
            }
            List<string> names = new List<string>
                                     {
                                         "BodyAnimDef_" + this.Pawn.def.defName + "_" + bodyType,
                                         "BodyAnimDef_" + ThingDefOf.Human.defName + "_" + bodyType
                                     };
            foreach (string name in names)
            {
                BodyAnimDef newDef = DefDatabase<BodyAnimDef>.GetNamedSilentFail(name);
                if (newDef != null)
                {
                    this.bodyAnim = newDef;
                    return;
                }
            }

            this.bodyAnim = new BodyAnimDef { defName = this.Pawn.def.defName, label = this.Pawn.def.defName };
        }

        public void TickDrawers(Rot4 bodyFacing, PawnGraphicSet graphics)
        {
            if (!initialized)
            {
                this.InitializePawnDrawer();
                initialized = true;
            }

            if (!this.PawnBodyDrawers.NullOrEmpty())
            {
                int i = 0;
                int count = this.PawnBodyDrawers.Count;
                while (i < count)
                {
                    this.PawnBodyDrawers[i].Tick(bodyFacing, graphics);
                    i++;
                }
            }
        }

        public List<Material> UnderwearMatsBodyBaseAt(Rot4 facing, RotDrawMode bodyCondition = RotDrawMode.Fresh)
        {
            int num = facing.AsInt + 1000 * (int)bodyCondition;
            if (num != this.cachedSkinMatsBodyBaseHash)
            {
                this.cachedSkinMatsBodyBase.Clear();
                this.cachedSkinMatsBodyBaseHash = num;
                PawnGraphicSet graphics = this.Pawn.Drawer.renderer.graphics;
                if (bodyCondition == RotDrawMode.Fresh)
                {
                    this.cachedSkinMatsBodyBase.Add(graphics.nakedGraphic.MatAt(facing));
                }
                else if (bodyCondition == RotDrawMode.Rotting || graphics.dessicatedGraphic == null)
                {
                    this.cachedSkinMatsBodyBase.Add(graphics.rottingGraphic.MatAt(facing));
                }
                else if (bodyCondition == RotDrawMode.Dessicated)
                {
                    this.cachedSkinMatsBodyBase.Add(graphics.dessicatedGraphic.MatAt(facing));
                }

                for (int i = 0; i < graphics.apparelGraphics.Count; i++)
                {
                    ApparelLayer lastLayer = graphics.apparelGraphics[i].sourceApparel.def.apparel.LastLayer;

                    // if (lastLayer != ApparelLayer.Shell && lastLayer != ApparelLayer.Overhead)
                    if (lastLayer == ApparelLayer.OnSkin)
                    {
                        this.cachedSkinMatsBodyBase.Add(graphics.apparelGraphics[i].graphic.MatAt(facing));
                    }
                }
            }

            return this.cachedSkinMatsBodyBase;
        }

        #endregion Public Methods

    }
}