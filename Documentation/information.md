[to Overview](./overview.md)

---
# Information
Uses `=` prefix. A piece of information

## Formatting
`= <text>`
* `text` can be any string
 
### Example
> ```
> = some information
> ```

## Information Nesting
Information can be nested inside of [paths](./path.md), [object](./object.md), [curiosities](./curiosity.md), and Information. Information can self nest itself.

### Examples
> ```
> + some object
>	= information about the object
> ```
> ```
> + another object
>	= this object glows
>	   = the object does not glow if it's night
>	      = the object will flicker during the time between night and day
> ```
> ```
> = walking past the large door will make a random event occur
>	= the player dies immediately
>	= the player gains 100 extra health
>	   = the player cannot heal above their max health 
> ```