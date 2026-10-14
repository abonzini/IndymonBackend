namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
    {
        public const int SHINY_CHANCE = 500; /// Shiny chance of 1 in 500
        public const int DEFAULT_BATTLE_NMONS = 3; /// Tournaments are 3v3 and only 3 mon total per team, same for wild mons
        public const double NPC_ITEM_CHANCE = 1.5; /// Npcs with name and items have a 0.5 chance of equipping one of them to a mon and a guaranteed one
        public const int DEFAULT_JEWELRY_DURATION = 3; /// Unsure how I'll deal with jewelry, default 3
        public const int AMULET_COIN_IMP_MULT = 2; /// Amulet coin has a 2x multiplier to cashhh (effect is resolved in prizes unfort)
        public const int TRAINER_SALARY = 2; /// IMP Salary for trainers in tournament
        public const int DUNGEON_NUM_FLOORS = 3; /// Each dungeon will always have 3 floors
        public const int DUNGEON_ROOMS_PER_FLOOR = 5; /// Each dungeon floor has 5 rooms (some hardcoding too)
        public const int MAX_SIMULTANEOUS_ROOM_PICKS = 5; /// How many rooms max we'll get per room choice
        public const int DUNGEON_ENCOUNTER_ROOM = 1; /// Pokemon encounter happens in 2nd room
        public const int DUNGEON_BOSS_ROOM = 3; /// Boss encounter happens in 4th room...
        public const int DUNGEON_FINAL_BOSS_ROOM = 4; /// ...except final boss which is in last room of dung
    }
}

