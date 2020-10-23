﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> An object that can be connected to other 
/// objects of the same kind via road_links. </summary>
public class settler_path_element : MonoBehaviour, INonBlueprintable, INonEquipable
{
    public settler_path_link[] links { get; private set; }

    public List<settler_path_element> linked_elements()
    {
        List<settler_path_element> ret = new List<settler_path_element>();
        foreach (var l in links)
            if (l.linked_to != null)
            {
                var rl = l.linked_to.GetComponentInParent<settler_path_element>();
                if (rl != null)
                    ret.Add(rl);
            }
        return ret;
    }

    bool start_called = false;
    private void Start()
    {
        // Register this element, if neccassary
        start_called = true;
        links = GetComponentsInChildren<settler_path_link>();
        register_element(this);
    }

    private void OnDestroy()
    {
        // Unregister this element, if neccassary
        if (start_called)
            forget_element(this);
    }

    void try_link(settler_path_element other)
    {
        // Can't link to self
        if (other == this) return;

        foreach (var l in links)
        {
            // L already linked
            if (l.linked_to != null) continue;

            foreach (var l2 in other.links)
            {
                // L2 already linked
                if (l2.linked_to != null) continue;

                if ((l.transform.position - l2.transform.position).magnitude <
                    settler_path_link.LINK_DISTANCE)
                {
                    // Make link both ways
                    l.linked_to = l2;
                    l2.linked_to = l;
                    break;
                }
            }
        }
    }

    void break_links()
    {
        foreach (var l in links)
        {
            if (l.linked_to != null)
            {
                l.linked_to.linked_to = null;
                l.linked_to = null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var l in links)
            if (l.linked_to != null)
                Gizmos.DrawLine(transform.position, l.transform.position);
    }

    float heuristic(settler_path_element other)
    {
        return (transform.position - other.transform.position).magnitude;
    }

    //##############//
    // STATIC STUFF //
    //##############//

    static HashSet<settler_path_element> all_elements;

    public static settler_path_element find_nearest(Vector3 position)
    {
        return utils.find_to_min(all_elements,
            (r) => (r.transform.position - position).sqrMagnitude);
    }

    public static void initialize()
    {
        // Initialize theelements collection
        all_elements = new HashSet<settler_path_element>();
    }

    static void validate_links(settler_path_element r)
    {
        // Re-make all links to/from r
        r.break_links();
        foreach (var r2 in all_elements)
            r.try_link(r2);
    }

    static void register_element(settler_path_element r)
    {
        // Create links to/from r, add r to the collection of elements.
        validate_links(r);
        if (!all_elements.Add(r))
            throw new System.Exception("Tried to register element twice!");
    }

    static void forget_element(settler_path_element r)
    {
        // Forget all the links to/from r, remove r from the collection of elements
        r.break_links();
        if (!all_elements.Remove(r))
            throw new System.Exception("Tried to forget unregistered element!");
    }

    /// <summary> Find a path between the start and end elements, using 
    /// the A* algorithm. Returns null if no such path exists. </summary>
    public static List<settler_path_element> path(settler_path_element start, settler_path_element goal)
    {
        // Setup pathfinding state
        var open_set = new HashSet<settler_path_element>();
        var closed_set = new HashSet<settler_path_element>();
        var came_from = new Dictionary<settler_path_element, settler_path_element>();
        var fscore = new Dictionary<settler_path_element, float>();
        var gscore = new Dictionary<settler_path_element, float>();

        // Initialize pathfinding with just start open
        open_set.Add(start);
        gscore[start] = 0;
        fscore[start] = goal.heuristic(start);

        while (open_set.Count > 0)
        {
            // Find the lowest fscore in the open set
            var current = utils.find_to_min(open_set, (c) => fscore[c]);

            if (current == goal)
            {
                // Success - reconstruct path
                List<settler_path_element> path = new List<settler_path_element> { current };
                while (came_from.TryGetValue(current, out current))
                    path.Add(current);
                path.Reverse();
                return path;
            }

            // Close current
            open_set.Remove(current);
            closed_set.Add(current);

            foreach (var n in current.linked_elements())
            {
                if (closed_set.Contains(n))
                    continue;

                // Work out tentative path length to n, if we wen't via current
                var tgs = gscore[current] + n.heuristic(current);

                // Get the current neighbour gscore (infinity if not already scored)
                if (!gscore.TryGetValue(n, out float gsn))
                    gsn = Mathf.Infinity;

                if (tgs < gsn)
                {
                    // This is a better path to n, update it
                    came_from[n] = current;
                    gscore[n] = tgs;
                    fscore[n] = tgs + goal.heuristic(n);
                    open_set.Add(n);
                }
            }
        }

        // Pathfinding failed
        return null;
    }
}