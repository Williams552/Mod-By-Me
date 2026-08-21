using System.Collections.Generic;
using Verse;

namespace RimwardExiles.Core
{
    public enum ModPath
    {
        None,
        Steel,
        Flesh,
        Blood,
        Purity
    }

    public class BodyPathEntry
    {
        public string hediff;
        public ModPath path;
    }

    public class PackageIdRule
    {
        public string packageIdContains;
        public ModPath path;
    }

    public class HeroBodyPathDef : Def
    {
        public List<BodyPathEntry> entries = new List<BodyPathEntry>();
        public List<PackageIdRule> packageIdRules = new List<PackageIdRule>();
    }
}
