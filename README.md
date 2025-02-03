# MonMulti
Work-In-Progress multiplayer mod for the game Mon Bazou

## Working:
Server starts when the user enters a save

Client connects to the defined address when user enters a save

**↳These will be merged into a single code & dll after i manage to establish a good enough connection between Client & Server (For now server is a separate code, so i dont have to run the game twice to test!)**

The *Client* and *Server* are both running asynchronous so they dont freeze and crash the game running on the main thread.

## WIP:

Showing connected players as game objects (capsules),

Syncing ***Money***,***Time***, Maybe FriendShips...
