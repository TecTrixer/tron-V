= Project Proposal - Computer Graphics 457

We (Marius Tomek and Constantin Mussaeus) are proposing the game *Tron-V*:

== Project Description

We plan to build a 3D survival multiplayer game. A player can connect to the server and join a game. He will be able to control a motorbike which leaves a trail behind in the style of Tron motorbikes. If another player collides with that trail he will be eliminated. The last player to survive will win the game.

Additionally, there will be a diverse range of different power having different effects. E.g., a power up can make a player invincible for a short time, it can give them a speed boost, it can spawn a PacMan, which will follow other players and eliminate them on collision, and it can spawn a Mario-like plant, which spits fireballs.

== Key learnings and techniques 

We use a variety of different topics learned during the class:

/ Basic 3D game programming: Similar to project 1, we need to design levels and implement the game logic in Unity.
/ Modelling: We need to model the motorbike and the other effects as well as the arena. Unlike in the course, we will likely not use Surface of Revolution though. 
/ Color Materials: In order to achieve the Tron style, we need to design a highly emissive color material using the color model learned in this class.
/ Physics: We will not be reimplementing the physics model but we will still use it for moving the motorbikes and handling collisions.

== Programming Languages, other Tools

We will be using the Unity 3D engine together with the Mirror Networking package for multiplayer. We will take the 3D assets from the asset store and design the other ones using Blender.

== Development Plan

At first, we need to establish a working multiplayer connection such that one can open a new instance of the client and it automatically connects with a server. Afterwards, we can start to implement the basic game logic including the player movement and the trail and collision modelling. This version is the minimum viable game, which should be done after 1 week, and from there we can start to add more features. Those additional features include different power ups and different arena designs. 

== Work Split

We plan to split the work evenly in such a way that both of us work on all parts of the project. Therefore, we will both work on the game logic, the game design and the modelling aspect.

== Working prototype

We already made a working multiplayer demonstration prototype using Mirror Networking and Unity. This prototype enables different clients to connect to each other, move around a plane and see each others movements and messages.
