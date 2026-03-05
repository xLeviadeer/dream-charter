[to Overview](./overview.md)

---
# Getting Started
* [Setting up a Note-Set](#setting-up-a-note-set)
* [Creating a Location](#creating-a-location)
* [Creating a Common Object](#creating-a-common-object)
* [Curiosities](#curiosities)
* [Basics of the CLI](#basics-of-the-cli)
* [Finding Paths](#finding-paths)
* [?, i & a](#i-a)

## Setting up a Note-Set
This step will already be completed if you have just downloaded the program. But in the case that it is not for some reason, or you are creating a new project you must complete this step. You can continue to ⟨Creating a Location⟩ if this step is complete.

To set up a new note-set ⌄
1. Run the program (Dream Charter.exe). This will give you an error (stating that 'overworld must exist'. Leave the program open for now. 
2. In your files you will see new folders, including `data`. Open the `data` folder. The `data` folder is meant to contain your notes.
3. In this folder create a file named `overworld.dreamlocation`. Inside of this file write the text `overworld`.

You can open your `.dreamlocation` folder with a text editor like Notepad or Visual Studio Code.

If you'd like you can [change the default location](./default-location.md) to something other than ⸉overworld⸉. In this case you'd want to replace the text in `default_location` to `‹your_name›`, `overworld.dreamlocation` with `‹your_name_›.dreamlocation` and the text in that file to `‹your_name›`.

From here you can add fields to your ⸉overworld⸉ location note like [information](./information.md), [objects](./object.md) and [paths](./path.md). An example ⸉overworld⸉ is below ⌄
> ```
> overworld
> = this is the overworld and starting location
> * mysterious glass bottle
>	= it says the bottle is needed later in the game
> + beach ball
>	= you can sit in the sand and play with a beach ball
> ```

Once you have a starting location (overworld) then you can press enter on the program to reload it and continue.

---
## Creating a Location
### Creating a New Location
To create a new location create a new file in the data folder with some name with the `.dreamlocation` extension. For example, `my location.dreamlocation`. In this file you need to an ⸉id⸉. It should match your file name╌so╌`my location` should be the contents of the file.

### Creating a Path to your Location
Now that you have another location you can add a [path](./path.md) to this new location. Inside of `overworld` or any other location create a [path](./path.md). `- my direction -> my location`. This will one-way link overworld to my location with the directions ⸉my direction⸉.

Paths can have multiple directions in order to indicate a sequence of actions that needs to be completed. An example is `- my first direction -> my second direction -> my location`.

You can add more paths inside of other locations, creating a map of different locations.

### Example
The example project⊶[locations](./Examples/locations)⊷is an example with the described linked locations.

---
## Creating a Common Object
### Creating a New Common Object
To create a new common object create a new new file in the data folder with some name with the `.dreamobject` extension. For example, `my object.dreamobject`. In this file you need to an ⸉id⸉. It should match your file name╌so╌`my object` should be the contents of the file. 

### Creating a Common Object Reference
Now that you have a common object you can add an [common object reference](./common-object.md) to any location. In your location╌on a new line╌add `^` followed by the id of your object (`my object`). 

### Example
The example project⊶[common-objects](./Examples/common-objects)⊷is an example with the described common object.

---
## Curiosities
### Why Curiosities?
Curiosities are a specific type of information which is stored like a ⸉todo⸉ item. Curiosities are used to list things that are not yet understood; curiosities are a log of things you'd like to get to but can't at the moment.

Curiosities should be used over regulary information because they can specifically be listed in a list of curiosities rather than clogging up information fields with todo items.

### Creating a Curiosity
In any location╌on a new line╌add `*` followed by whatever you'd like to write for this curiosity.

### Example
The example⊶[curiosity](./Examples/curiosity)⊷shows a curiosity.

--- 
## Basics of the CLI
When the program is ran a basic interface showing a set of possible options will appear. There are two types of options: ⸉index selectable options⸉ and ⸉exploration⸉ actions.

Index selectable options can be accessed by typing the associated number. Usually, a submenu with more index selectable options will appear. Each option has an explanation of what the option does next to it. 

Most notably, one of the options is ⸉reload⸉. Reload will try to get read all of your notes and update the CLI with the new information. If╌at any point╌during loading your notes the program encounters and error it will put you into ⸉lockdown mode⸉ where the program idles, displays an error and waits for you to resolve it. Once the error is resolved press enter to try to reload again.

Exploration actions facilitate the core idea of using the program. The idea is that you are ⸉in⸉ a location (starting in the overworld). You can then virtually ⸉explore⸉ your notes by location and create a path history of your previously visited locations. You can also ask the program to find paths of various specifications using your notes. You will be shown all of the data on your ⸉current⸉ location which lets you know what your possible exploration options are. 

Exploration actions are inputs of specific formats to perform certain actions. All exploration actions are useful, but the most integral ones are outlined ⌄
* locationId based travel
	* type the id of a location which can be travelled to from the current location to go there. 
	* You can also type a list of locations to travel to in succession by starting with a `>` and joining every location with another `>`.
* objectId reference based viewing
	* type the id of a common object reference within the current location to see it's info
* going back
	* use `..` to go back. You can add more `.`s to go back further.
	* use `,,` to go back via the [exit path](./exit-keyword.md).
	* use `//` to go to your default(|home) location (overworld by default).

### Example Navigating
Given the following two locations ⌄
> ```
> overworld
> - sit on the couch -> couch land
> ```
> ```
> couch land
> ^ apple
>	= lost apple can be found in the couch
> + coin
>	= lost coin can be found in the couh
> - wake up -> overworld
> - fall off the couch -> the void
> ```
> ```
> the void
> - move far enough away from the center -> overworld
> ```
and the following common object
> ```
> apple
> = restores 5 health
> ```
all of the following are valid navigations from overworld. This example can be found in [comprehensive-example](./Examples/comprehensive-example).

1. `couch land`
	* brings you into couch land | changes your current location to couch land
2. `> couch land > the void`
	* brings you into couch land then the void | changes your current location to the void
3. `couch land` then `apple` 
	* brings you into couch land | changes your current location to couch land
	* shows the common object information about apple
4. `couch land` then `,,` 
	* brings you into couch land | changes your current location to couch land
	* brings you into overworld via ⸉wake up⸉.
5. `couch land` then `..` 
	* brings you into couch land | changes your current location to couch land
	* brings you into overworld by removing ⸉couch land⸉ from the end of your path history.
6. `couch land` then `,,` 
	* brings you into couch land | changes your current location to couch land
	* brings you into overworld by reseting your path and bringing you to the default location.

---
## Finding Paths
Pathfinding works by building a path separate to your current path | pathfinding is non-destructive to your current path unless you select `s` when exiting pathfinding.

You will specify a start and stop location and a pathfinding method continually until you say to stop building a path.

1. type `4` to enter pathfinding.
2. type the location id, `//` or `.` to set the starting location. 
3. type the location id, `//` or `.` to set the ending location.
	* ending locations can also take an objectId, which tells the pathfinder to get a path to the nearest (cheapest or bounded) location with the specified objectId.
4. use a number to select what pathfinding mode to use.
	* the program will attempt to find a path with the specifications you've given. If it cannot step 5 will be skipped.
5. choose whether or not you want to add the path to the path you are currently building with `y` or `n`.
	* if you choose `y` then the path that was found will be added to the path being build╌the ⸉fullpath⸉.
6. choose whether or not to continue building a path.
	* using `y` will continue having you select places to move to.
	* using `n` will exit pathfinding.
	* using `s` will exit pathfinding and set the ⸉fullpath⸉ to your current path.

---
## `?`, `i` & `a`
Paths can lead to [reserved locations](./path.md) which are described in⊶[paths](./path.md)⊷.

The purpose of using each reserved location is ⌄ with an example of when this would happen
* `?` for when you find a location you know the path to but not where it leads. An example would be finding a door but not entering it to know where it goes yet.
* `i` for infinite or random locations. An example would be a door which takes you to a random location.
* `a` for any location. An example would be a door which takes you to any location in the game you choose.

Examples of paths going to reserved locations ⌄
* `- exit the door -> ?`
* `- spin in 10 circles -> i`
* `- touch glowing orb -> a`

---
Done getting started? [head back to Overview](./overview.md).