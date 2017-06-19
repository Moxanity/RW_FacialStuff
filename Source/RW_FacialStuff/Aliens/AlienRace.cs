﻿using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RW_FacialStuff
{
    public class AlienRace
    {
        public bool UseMelaninLevels
        {
            get;
            set;
        }

        public bool HasSecondaryColor
        {
            get;
            set;
        }

        public List<Color> PrimaryColors
        {
            get;
            set;
        }

        public List<Color> SecondaryColors
        {
            get;
            set;
        }

        public List<Color> HairColors
        {
            get;
            set;
        }

        public List<BodyType> BodyTypes
        {
            get;
            set;
        }

        public List<string> CrownTypes
        {
            get;
            set;
        }

        public bool GenderSpecificHeads
        {
            get;
            set;
        }

        public string GraphicsPathForHeads
        {
            get;
            set;
        }

        public bool HasHair
        {
            get;
            set;
        }

        public HashSet<string> HairTags
        {
            get;
            set;
        }

        public bool RestrictedApparelOnly
        {
            get;
            set;
        }

        public HashSet<string> RestrictedApparel
        {
            get;
            set;
        }
    }
}