namespace MechanicsData
{
    public enum MoveFlag
    {
        BANNED, // Forbidden moves
        DOUBLES_ONLY, // Moves that are good in doubles only, do not use in singles
        DRAIN, // Draining moves, benefit from root
        HEAL, // MOves that heal the user (triage)
        CHANCE, // Moves with a chance of something nice, for serene grace
        PIVOT, // Pivot moves (but not stuff like memento)
        SOUND, // Sound moves
        DEFENSE_DAMAGE, // Body press lol
        MULTI_HIT, // Multihit moves
        PUNCH, // Punching moves
        SHARP, // Sharpness
        DANCE, // Dance moves
        CONTACT, // Contact moves
        BITING, // Biting moves
        RECOIL, // Recoil moves
        RECKLESS, // Recoil + Crash damage
        LAUNCHER, // Launcher moves
        EXPLOSIVE, // Explosive moves
        PRIORITY, // Prio
        SELF_SLEEP, // Moves that need user to be sleeping
        SELF_STATUS, // Moves that benefit from user to be statused
        OPP_SLEEP, // Moves that need opponent to be asleep
        OPP_PARALYSIS, // Moves that require opponent to be paralyzed
        GIVE_ITEM, // Moves that give item to enemy and/or discard item from owner,
        CRITICAL, // Sniper
        NEED_OPP_ITEM, // Moves that need opp to have an item to be useful
        POSITIVE_STAT_BOOST, // Moves that appreciate many stat boosts
        SETUP_STATUS // Setup status moves
    }
    public enum MoveCategory
    {
        PHYSICAL,
        SPECIAL,
        STATUS
    }
}
