Isometric2DGame – Trial Task (Programming - RDE)

Overview

This project was developed as part of the Runic Dices Entertainment
(RDE) Programming Trial Task.
It is a small isometric 2D action-adventure prototype demonstrating: -
Player movement with the Unity Input System - AI state machine for
enemies - Combat system (melee attacks, hit feedback) - Inventory & item
usage - NPC dialogue and quest system

Unity Version: 6000.1.4f1 (URP 2D Template)
Created and tested on Unity 6.0.1f1

------------------------------------------------------------------------

How to Test the Game

1.  Open the project in Unity Editor.
    -   Default scene: SampleScene.unity
    -   Press Play to start.
2.  Player Controls
    -   W / A / S / D – Move
    -   Mouse Left – Attack
    -   E – Interact (talk to NPCs / pick up items)
    -   I – Toggle Inventory
    -   ESC – Close inventory or dialogue
3.  Gameplay Flow
    -   Talk to the NPC with E to start a quest.
    -   The quest requires you to defeat 3 enemies.
    -   Track your progress in the Quest Tracker.
    -   When finished, return to the NPC for your reward.
    -   Collect items and open the inventory (I).
    -   Right-click consumable items to heal yourself.
4.  Combat and Health
    -   Both player and enemies have HP bars.
    -   Damage displays with animated floating numbers.
    -   Enemies drop collectible items when they die.

------------------------------------------------------------------------

Features

  -----------------------------------------------------------------------
  Category                        Description
  ------------------------------- ---------------------------------------
  Movement                        Rigidbody2D-based movement,
                                  acceleration/deceleration, sprite flip

  Camera                          Dead zone follow system for smooth
                                  tracking

  Enemy AI                        Finite State Machine (Idle, Patrol,
                                  Chase, Attack)

  Combat                          Melee attacks, cooldowns, hit feedback,
                                  enemy knockback

  Health System                   Player & enemies with delayed damage
                                  bar and death animation

  Inventory                       Collect, stack, use (heal), and drag &
                                  drop items

  World Items                     Floating pickups with tooltips (“Press
                                  E to pick up”)

  Dialogue & Quest                Interactive NPCs, quest tracking,
                                  reward delivery
  -----------------------------------------------------------------------

------------------------------------------------------------------------

Assets Used (Free on Unity Asset Store)

-   Hero Knight – Pixel Art Character
    (used for player character and animations)
-   Monsters Creatures Fantasy – Pixel Art
    (used for enemy sprites)
-   RPG Icons Free Starter Pack
    (used for item and UI icons)
-   32×32 Isometric Tileset Pack
    (used for environment and level layout)

All assets are free and used under their respective Unity Asset Store
license.

------------------------------------------------------------------------

Known Issues / Notes

-   Player death does not reset the scene yet.
-   Dialogue system currently supports one active quest at a time.
-   Isometric camera implemented manually.

------------------------------------------------------------------------

Credits

-   Code & Systems: Fazıl Ufuk Kırmızı
-   Assets: Listed above (all free Unity Store packs)
-   UI: TextMeshPro & Unity built-in components

------------------------------------------------------------------------

License

This project was created solely for evaluation purposes as part of the
RDE Trial Task and is not intended for commercial release.
