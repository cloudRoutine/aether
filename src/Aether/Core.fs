﻿[<AutoOpen>]
module Aether.Core

(* Types

   Types defining lenses and isomorphisms (both total
   and partial as standard pairs. These can be implemented implicitly,
   so an assembly *providing* lenses without also consuming them
   requires no dependency on Aether, just an implicit structuring. *)

/// Total lens from a -> b
type Lens<'a,'b> = ('a -> 'b) * ('b -> 'a -> 'a)

/// Partial lens from a -> b
type PLens<'a,'b> = ('a -> 'b option) * ('b -> 'a -> 'a)

// Isomorphisms

/// Total isomorphism of a <> b
type Iso<'a,'b> = ('a -> 'b) * ('b -> 'a)

/// Partial isomorphism of a <> b
type PIso<'a,'b> = ('a -> 'b option) * ('b -> 'a)

(* Functions

   Functions for using lenses to get, set and modify values within a target
   instance. *)

/// Get a value using a total lens
let getL ((g, _): Lens<'a,'b>) = 
    fun a -> g a

/// Get a value option using a partial lens
let getPL ((g, _): PLens<'a,'b>) = 
    fun a -> g a

/// Get a value or a default using a partial lens
let getOrPL ((g, _): PLens<'a,'b>) = 
    fun b a -> g a |> function | Some b -> b | _ -> b

/// Set a value using a total lens
let setL ((_, s): Lens<'a,'b>) =
    fun b a -> s b a

/// Set a value using a partial lens
let setPL ((_, s): PLens<'a,'b>) =
    fun b a -> s b a

/// Modify a value using a total lens
let modL ((g, s): Lens<'a,'b>) = 
    fun f a -> s (f (g a)) a

/// Modify a value using a partial lens
let modPL ((g, s): PLens<'a,'b>) = 
    fun f a -> Option.map f (g a) |> function | Some b -> s b a | _ -> a

(* Compositions

   Functions for composing lenses and isomorphisms, each of which
   returns a new lens of a total or partial type based on the lenses
   or isomorphisms composed. It is more common (and significantly less
   verbose) to use the infix operator forms of these compositions (though note
   that Aether.Operators is not open by default and should be opened explicitly). *)

/// Compose a total lens and a total lens, giving a total lens
let composeLL ((g1, s1): Lens<'a,'b>) ((g2, s2): Lens<'b,'c>) : Lens<'a,'c> =
    (fun a -> g2 (g1 a)),
    (fun c a -> s1 (s2 c (g1 a)) a)

/// Compose a total lens and a partial lens, giving a partial lens
let composeLPL ((g1, s1): Lens<'a,'b>) ((g2, s2): PLens<'b,'c>) : PLens<'a,'c> =
    (fun a -> g2 (g1 a)),
    (fun c a -> s1 (s2 c (g1 a)) a)

/// Compose a partial lens and a total lens, giving a partial lens
let composePLL ((g1, s1): PLens<'a,'b>) ((g2, s2): Lens<'b,'c>) : PLens<'a,'c> =
    (fun a -> Option.map g2 (g1 a)),
    (fun c a -> Option.map (s2 c) (g1 a) |> function | Some b -> s1 b a | _ -> a)

/// Compose two partial lenses, giving a partial lens
let composePLPL ((g1, s1): PLens<'a,'b>) ((g2, s2): PLens<'b,'c>) : PLens<'a,'c> =
    (fun a -> Option.bind g2 (g1 a)),
    (fun c a -> Option.map (s2 c) (g1 a) |> function | Some b -> s1 b a | _ -> a)

/// Compose a total lens with a total isomorphism, giving a total lens
let composeLI ((g, s): Lens<'a,'b>) ((f, t): Iso<'b,'c>) : Lens<'a,'c> =
    (fun a -> f (g a)),
    (fun c a -> s (t c) a)

/// Compose a total lens with a partial isomorphism, giving a partial lens
let composeLPI ((g, s): Lens<'a,'b>) ((f, t): PIso<'b,'c>) : PLens<'a,'c> =
    (fun a -> f (g a)),
    (fun c a -> s (t c) a)

/// Compose a partial lens with a total isomorphism, giving a partial lens
let composePLI ((g, s): PLens<'a,'b>) ((f, t): Iso<'b, 'c>) : PLens<'a,'c> =
    (fun a -> Option.map f (g a)),
    (fun c a -> s (t c) a)

/// Compose a partial lens with a partial isomorphism, giving a partial lens
let composePLPI ((g, s): PLens<'a,'b>) ((f, t): PIso<'b,'c>) : PLens<'a,'c> =
    (fun a -> Option.bind f (g a)),
    (fun c a -> s (t c) a)

(* Lenses

   Various lenses implemented for common types such as tuples,
   lists and maps, along with an id lens (which is useful for composing
   a lens which has not specific "lensing" elements but is implicitly a chain
   of one or more isomorphisms. Having an id lens enables the root composition. *)

/// Identity lens returning the original item regardless of modifiction
let idLens : Lens<'a,'a> =
    (fun x -> x), (fun _ x -> x) 

/// First item of a tuple giving a total lens
let fstLens : Lens<('a * 'b),'a> =
    fst, (fun a t -> a, snd t)
        
/// Second item of a tuple giving a total lens
let sndLens : Lens<('a * 'b),'b> =
    snd, (fun b t -> fst t, b)

/// Head of a list giving a partial lens
let headPLens : PLens<'v list, 'v> =
    (function | h :: _ -> Some h | _ -> None),
    (fun v -> function | _ :: t -> v :: t | l -> l)

/// Position of a list giving a partial lens
let listPLens (i: int) : PLens<'v list, 'v> =
    (function | l when List.length l > i -> Some (List.nth l i) | _ -> None), 
    (fun v l -> List.mapi (fun i' x -> if i = i' then v else x) l)

/// Tail of a list giving a partial lens
let tailPLens : PLens<'v list, 'v list> =
    (function | _ :: t -> Some t | _ -> None),
    (fun t -> function | h :: _ -> h :: t | [] -> t)

/// Key of a map giving a partial lens
let mapPLens (k: 'k) : PLens<Map<'k,'v>,'v> =
    Map.tryFind k, Map.add k