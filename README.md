# KalsiumWork

KalsiumWork is a modifier and event based game framework.

## Concepts

**Coordinator**: Handles running of the game.  
**Game**: Represents the global game state. Game behaviour modifiable by GameModifiers.  

**Master**: Base for all classes with Modifiers (Units, Buildings)  
**Modifier**: Modifies behaviour and Attributes of Masters (Stun, Regeneration)  

**Attribute**: Define values of Masters (Health, Position)  
**Alterer**: Modify values of Attributes. Automatically updated if used correctly (I forgot).  

**IOnEvents**: Events or hooks that allow reaction to changes in game state.  


## Goals

- Modify and extend behaviour (Modifiers)
- Modify and update values automatically (Attributes and Alterers)
- React to things happening in game (IOnEvents)
- Use Update and LateUpdate only for visuals and UI interactions
- Online (not even WIP)
    - Server
        - Logic
        - Send visual state updates
    - Client
        - UI
        - Visual state