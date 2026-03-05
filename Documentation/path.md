[to Overview](./overview.md)

---
# Paths
Uses prefix `-`. A ⸉location reference⸉. Paths must have at least 1 direction and end with a valid(|existing) location.

## Formatting
`"- "(<step_to_achieve_path>" -> ")*"-> "<path_to_location>`
* `steps_to_achieve_path` can be any string 
* `path_to_location` is expected to be a valid dreamlocation id

### Examples
> ```
> - fall off map -> special place
> ```
> ```
> - climb cliff -> enter house -> enter door -> special place 2
> ```

## Information Nesting
[Information](./information.md) can be nested inside of a path

### Example
> ```
> - some direction -> some path
>	= information here
> ```

## Reserved Locations
Reserved locations always exist and cannot have files created with identical IDs as them. They are locations which are reserved for special purposes.

Reserved locations ⌄
* `?` — Unknown
	* Meaning: That this location is unknown. This can be used when the path to get somewhere is known but the ending location isn't
	* Pathable: No, unknown cannot be pathed to
	* Pathfindable: No, unknown cannot be used during pathfinding
	* Info: You may prefer to use [curiosities](./curiosity.md) instead for unknown locations. It's up to you what you prefer.
* `i` — Infinite
	* Meaning: This location leads to infinite locations or an impossible location.
	* Pathable: Yes, infinite can be pathed to. Pathing to infinite can lead to any location that exists.
	* Pathfindable: No, infinite cannot be used during pathfinding
* `a` — Anywhere
	* Meaning: This location leads to any location. 
	* Pathable: Yes, anywhere can be pathed to. Pathing to anywhere can lead to any location that exists.
	* Pathfindable: Yes, anywhere can be used during pathfinding and leads to any location that exists.

### Example
> ```
> - enter secret door -> ?
> ```

## Costs
Paths may be given a 'cost'. A cost should be denoted with a # followed by numbers (of format `"#"[0-9]+`). Costs can only be included inside of direction fields or anonymously. A cost determines how expensive it is to take a certain route. This is included so the pathfinding algorithm can determine how to choose the cheapest path instead of only the quickest one.

### Examples
> ```
> - #10 10 key door -> a location
> ```
> ```
> - do something difficult -> #5 -> a location
> ```