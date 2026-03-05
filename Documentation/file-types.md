[to Overview](./overview.md)

---
# File Types
Common location and common object files are used to denote travel between locations as well as information about locations and the objects part of them. 

## Common Location
Uses extension `.dreamlocation` by default. Extension can be changed in `common_location_extension.txt`.

A **Common Location** (aka 'location') is the only type of location. A common location is a world in the game. Common locations can include any field: [path](./path.md), [object](./object.md), [common object](./common-object.md), [curiosity](./curiosity.md) and,  [information](./information.md).

A location is expected to start with an 'id' which can be referenced in other locations for pathing.

A location expects a path which starts with 'wake up' to be the path that leads to the wake up location. Wake up doesn't need to be included, but should be when available.

'overworld' must be a location and is expected to be the default starting location. The overworld requirement name can be changed `default_location.txt`.

The 'wake up' keyword (which binds to the comma `,` exploration action) can be changed in `exit_keyword.txt`.

### Examples
> ```dreamlocation
> a dream location name
> - wake up -> overworld
> - pay 5#5 keys -> a different location
> + magic hat
> ^ fruit
> * something unexplored
> = this location is filled with water
> ```

## Common Objects {#common-object}
Uses extension `.dreamobject` by default. Extension can be changed in `common_object_extension.txt`.

**Common Objects** are objects which exist across multiple worlds. Common objects can only include the object name and following [information](./information.md) fields.

A common object is expected to start with an 'id' which can be referenced in locations for sharing.

### Examples
> ```dreamobject
> a common object name
> = does common things
> ```