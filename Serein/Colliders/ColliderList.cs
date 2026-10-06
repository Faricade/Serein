using Foster.Framework;
using System;
using System.Linq;

namespace Serein;

public class ColliderList : Collider
{
    public Collider[] Colliders { get; private set; }

    public ColliderList(params Collider[] colliders)
    {
#if DEBUG
        foreach (var c in colliders)
            if (c == null)
                throw new Exception("Cannot add a null Collider to a ColliderList.");
#endif
        this.Colliders = colliders;
    }

    public void Add(params Collider[] toAdd)
    {
#if DEBUG
        foreach (var c in toAdd)
        {
            if (Colliders.Contains(c))
                throw new Exception("Adding a Collider to a ColliderList that already contains it!");
            else if (c == null)
                throw new Exception("Cannot add a null Collider to a ColliderList.");
        }
#endif

        Collider[] newColliders = new Collider[Colliders.Length + toAdd.Length];
        for (int i = 0; i < Colliders.Length; i++)
            newColliders[i] = Colliders[i];
        for (int i = 0; i < toAdd.Length; i++)
        {
            newColliders[i + Colliders.Length] = toAdd[i];
            toAdd[i].Added(Entity);
        }
        Colliders = newColliders;
    }

    public void Remove(params Collider[] toRemove)
    {
#if DEBUG
        foreach (var c in toRemove)
        {
            if (!Colliders.Contains(c))
                throw new Exception("Removing a Collider from a ColliderList that does not contain it!");
            else if (c == null)
                throw new Exception("Cannot remove a null Collider from a ColliderList.");
        }
#endif

        Collider[] newColliders = new Collider[Colliders.Length - toRemove.Length];
        int at = 0;
        foreach (var c in Colliders)
        {
            if (!toRemove.Contains(c))
            {
                newColliders[at] = c;
                at++;
            }
        }
        Colliders = newColliders;
    }

    internal override void Added(Entity entity)
    {
        base.Added(entity);
        foreach (var c in Colliders)
            c.Added(entity);
    }

    internal override void Removed()
    {
        base.Removed();
        foreach (var c in Colliders)
            c.Removed();
    }

    public override float Width
    {
        get
        {
            return Right - Left;
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public override float Height
    {
        get
        {
            return Bottom - Top;
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public override float Left
    {
        get
        {
            float left = Colliders[0].Left;
            for (int i = 1; i < Colliders.Length; i++)
                if (Colliders[i].Left < left)
                    left = Colliders[i].Left;
            return left;
        }

        set
        {
            float changeX = value - Left;
            foreach (var c in Colliders)
                c.Position.X += changeX;
        }
    }

    public override float Right
    {
        get
        {
            float right = Colliders[0].Right;
            for (int i = 1; i < Colliders.Length; i++)
                if (Colliders[i].Right > right)
                    right = Colliders[i].Right;
            return right;
        }

        set
        {
            float changeX = value - Right;
            foreach (var c in Colliders)
                c.Position.X += changeX;
        }
    }

    public override float Top
    {
        get
        {
            float top = Colliders[0].Top;
            for (int i = 1; i < Colliders.Length; i++)
                if (Colliders[i].Top < top)
                    top = Colliders[i].Top;
            return top;
        }

        set
        {
            float changeY = value - Top;
            foreach (var c in Colliders)
                c.Position.Y += changeY;
        }
    }

    public override float Bottom
    {
        get
        {
            float bottom = Colliders[0].Bottom;
            for (int i = 1; i < Colliders.Length; i++)
                if (Colliders[i].Bottom > bottom)
                    bottom = Colliders[i].Bottom;
            return bottom;
        }

        set
        {
            float changeY = value - Bottom;
            foreach (var c in Colliders)
                c.Position.Y += changeY;
        }
    }

    public override Collider Clone()
    {
        Collider[] clones = new Collider[Colliders.Length];
        for (int i = 0; i < Colliders.Length; i++)
            clones[i] = Colliders[i].Clone();

        return new ColliderList(clones);
    }

    public override void Render(Camera camera, Color color)
    {
        foreach (var c in Colliders)
            c.Render(camera, color);
    }

    /*
     *  Checking against other colliders
     */

    public override bool Collide(Vector2 point)
    {
        foreach (var c in Colliders)
            if (c.Collide(point))
                return true;

        return false;
    }

    public override bool Collide(Rect rect)
    {
        foreach (var c in Colliders)
            if (c.Collide(rect))
                return true;

        return false;
    }

    public override bool Collide(Vector2 from, Vector2 to)
    {
        foreach (var c in Colliders)
            if (c.Collide(from, to))
                return true;

        return false;
    }

    public override bool Collide(Hitbox hitbox)
    {
        foreach (var c in Colliders)
            if (c.Collide(hitbox))
                return true;

        return false;
    }

    public override bool Collide(Grid grid)
    {
        foreach (var c in Colliders)
            if (c.Collide(grid))
                return true;

        return false;
    }

    public override bool Collide(Circle circle)
    {
        foreach (var c in Colliders)
            if (c.Collide(circle))
                return true;

        return false;
    }

    public override bool Collide(ColliderList list)
    {
        foreach (var c in Colliders)
            if (c.Collide(list))
                return true;

        return false;
    }
}
