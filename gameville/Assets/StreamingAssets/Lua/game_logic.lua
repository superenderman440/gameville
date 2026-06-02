-- Simple Minecraft-style world generation in Lua

function generate_world()
    for x = 0, 16 do
        for z = 0, 16 do
            SpawnBlock(x, 0, z)
            if (x + z) % 5 == 0 then
                SpawnBlock(x, 1, z)
            end
        end
    end

    Log("Lua world generated")
end
