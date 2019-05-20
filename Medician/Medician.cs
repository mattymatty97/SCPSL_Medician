using Smod2;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.Events;

namespace Medician
{
    [PluginDetails(
        name = "Medician",
        author = "The Matty",
        description = "add NTF medicians",
        id = "mattymatty.medician",
        SmodMajor = 3,
        SmodMinor = 2,
        SmodRevision = 0,
        version = "0.1.0"
            )]
    public class Medician : Plugin
    {
        public EventHandlers Handlers { get; set; }

        public override void Register()
        {
            AddConfig(new ConfigSetting("medician_medikits",4,true,"The number of Medikits that a medican spawns with"));
            AddConfig(new ConfigSetting("medician_medic_weapon","USP",true,"The ID of the medic weapon"));
            AddConfig(new ConfigSetting("medician_medic_multiplier",30,true,"The multiplier for medic damage"));
           
            Handlers = new EventHandlers(this);

            AddEventHandlers(Handlers, Priority.Low);
        }
        
        public override void OnEnable()
        {
            Info("Medician enabled!");
        }

        public override void OnDisable()
        {
            Info("Medician disabled!");
        }

    }
}
