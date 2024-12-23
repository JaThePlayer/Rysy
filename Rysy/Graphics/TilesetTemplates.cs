namespace Rysy.Graphics;

public static class TilesetTemplates {
    public static string? CreateTemplate(char id, string name) {
        switch (name) {
            case "better":
                return JadeBetterTemplate(id);
            case "alternate":
                return PixelatorAlternateTemplate(id);
        }

        return null;
    }
    
    public static string PixelatorAlternateTemplate(char id) => $"""
    <Tileset id="{id}" path="alternateTemplate">
        <!-- edges -->
        <!-- top -->
        <set mask="x0x-111-x1x" tiles="6,5; 7,5; 8,5; 9,5"/>
        <!-- bottom -->
        <set mask="x1x-111-x0x" tiles="6,10; 7,10; 8,10; 9,10"/>
        <!-- left -->
        <set mask="x1x-011-x1x" tiles="5,6; 5,7; 5,8; 5,9"/>
        <!-- right -->
        <set mask="x1x-110-x1x" tiles="10,6; 10,7; 10,8; 10,9"/>
    
        <!-- h pillar == -->
        <set mask="x0x-111-x0x" tiles="2,6; 2,7; 2,8; 2,9"/>
        <!-- v pillar left -->
        <set mask="x0x-011-x0x" tiles="1,6; 1,7; 1,8; 1,9"/>
        <!-- v pillar right -->
        <set mask="x0x-110-x0x" tiles="3,6; 3,7; 3,8; 3,9"/>
    
        <!-- v pillar || -->
        <set mask="x1x-010-x1x" tiles="6,2; 7,2; 8,2; 9,2"/>
        <!-- v pillar top -->
        <set mask="x0x-010-x1x" tiles="6,1; 7,1; 8,1; 9,1"/>
        <!-- v pillar bottom -->
        <set mask="x1x-010-x0x" tiles="6,3; 7,3; 8,3; 9,3"/>
    
        <!-- single tiles -->
        <set mask="x0x-010-x0x" tiles="1,1; 2,1; 1,2; 2,2"/>
    
        <!-- corner top left -->
        <set mask="x0x-011-x1x" tiles="4,4; 5,4; 4,5; 5,5"/>
        <!-- corner top right -->
        <set mask="x0x-110-x1x" tiles="10,4; 11,4; 10,5; 11,5"/>
        <!-- corner bottom left -->
        <set mask="x1x-011-x0x" tiles="4,10; 5,10; 4,11; 5,11"/>
        <!-- corner bottom right -->
        <set mask="x1x-110-x0x" tiles="10,10; 11,10; 10,11; 11,11"/>
    
        <!-- inside corner top left -->
        <set mask="111-111-110" tiles="1,3"/>
        <!-- inside corner bottom left -->
        <set mask="110-111-111" tiles="1,4"/>
        <!-- inside corner top right -->
        <set mask="111-111-011" tiles="2,3"/>
        <!-- inside corner bottom right -->
        <set mask="011-111-111" tiles="2,4"/>
    
        <!-- |== -->
        <set mask="110-111-110" tiles="11,7"/>
        <!-- _||_ -->
        <set mask="010-111-111" tiles="7,4"/>
        <!-- ==| -->
        <set mask="011-111-011" tiles="4,7"/>
        <!-- T||T -->
        <set mask="111-111-010" tiles="7,11"/>
    
        <!-- ???? -->
        <set mask="010-111-110" tiles="3,2"/>
        <!-- ???? -->
        <set mask="010-111-011" tiles="4,2"/>
        <!-- ???? -->
        <set mask="011-111-010" tiles="4,1"/>
        <!-- ???? -->
        <set mask="110-111-010" tiles="3,1"/>
        <!-- ???? -->
        <set mask="010-111-010" tiles="3,3"/>
        <!-- ???? -->
        <set mask="110-111-011" tiles="3,4"/>
        <!-- ???? -->
        <set mask="011-111-110" tiles="4,3"/>
    
        <!-- ???? -->
        <set mask="x0x-111-011" tiles="2,10"/>
        <!-- ???? -->
        <set mask="x0x-111-110" tiles="1,10"/>
        <!-- ???? -->
        <set mask="011-111-x0x" tiles="2,11"/>
        <!-- ???? -->
        <set mask="110-111-x0x" tiles="1,11"/>
        <!-- ???? -->
        <set mask="x11-011-x10" tiles="10,1"/>
        <!-- ???? -->
        <set mask="11x-110-01x" tiles="11,1"/>
        <!-- ???? -->
        <set mask="x10-011-x11" tiles="10,2"/>
        <!-- ???? -->
        <set mask="01x-110-11x" tiles="11,2"/>
    
        <!-- ???? -->
        <set mask="x0x-111-010" tiles="8,11"/>
        <!-- ???? -->
        <set mask="010-111-x0x" tiles="8,4"/>
        <!-- ???? -->
        <set mask="01x-110-01x" tiles="4,8"/>
        <!-- ???? -->
        <set mask="x10-011-x10" tiles="11,8"/>
    
        <!-- ???? -->
        <set mask="x0x-011-x10" tiles="6,4"/>
        <!-- ???? -->
        <set mask="x0x-110-01x" tiles="9,4"/>
        <!-- ???? -->
        <set mask="x10-011-x0x" tiles="6,11"/>
        <!-- ???? -->
        <set mask="01x-110-x0x" tiles="9,11"/>
    
        <set mask="padding" tiles="6,6; 7,6; 8,6; 9,6;  6,7; 6,8; 6,9;  9,7; 9,8; 9,9;  7,9; 8,9"/>
        <set mask="center" tiles="7,7; 8,7; 7,8; 8,8"/>
      </Tileset>
    """;
    
    public static string JadeBetterTemplate(char id) => $"""
    <Tileset id="{id}" path="subfolder/betterTemplate">
        <!-- edges -->
        <!-- top -->
        <set mask="x0x-111-x1x" tiles="6,5; 7,5; 8,5; 9,5"/>
        <!-- bottom -->
        <set mask="x1x-111-x0x" tiles="6,10; 7,10; 8,10; 9,10"/>
        <!-- left -->
        <set mask="x1x-011-x1x" tiles="5,6; 5,7; 5,8; 5,9"/>
        <!-- right -->
        <set mask="x1x-110-x1x" tiles="10,6; 10,7; 10,8; 10,9"/>
    
        <!-- h pillar == -->
        <set mask="x0x-111-x0x" tiles="2,6; 2,7; 2,8; 2,9"/>
        <!-- v pillar left -->
        <set mask="x0x-011-x0x" tiles="1,6; 1,7; 1,8; 1,9"/>
        <!-- v pillar right -->
        <set mask="x0x-110-x0x" tiles="3,6; 3,7; 3,8; 3,9"/>
    
        <!-- v pillar || -->
        <set mask="x1x-010-x1x" tiles="6,2; 7,2; 8,2; 9,2"/>
        <!-- v pillar top -->
        <set mask="x0x-010-x1x" tiles="6,1; 7,1; 8,1; 9,1"/>
        <!-- v pillar bottom -->
        <set mask="x1x-010-x0x" tiles="6,3; 7,3; 8,3; 9,3"/>
    
        <!-- single tiles -->
        <set mask="x0x-010-x0x" tiles="1,1; 2,1; 1,2; 2,2"/>
    
        <!-- corner top left -->
        <set mask="x0x-011-x1x" tiles="4,4; 5,4; 4,5; 5,5"/>
        <!-- corner top right -->
        <set mask="x0x-110-x1x" tiles="10,4; 11,4; 10,5; 11,5"/>
        <!-- corner bottom left -->
        <set mask="x1x-011-x0x" tiles="4,10; 5,10; 4,11; 5,11"/>
        <!-- corner bottom right -->
        <set mask="x1x-110-x0x" tiles="10,10; 11,10; 10,11; 11,11"/>
    
        <!-- inside corner top left -->
        <set mask="111-111-110" tiles="1,3"/>
        <!-- inside corner bottom left -->
        <set mask="110-111-111" tiles="1,4"/>
        <!-- inside corner top right -->
        <set mask="111-111-011" tiles="2,3"/>
        <!-- inside corner bottom right -->
        <set mask="011-111-111" tiles="2,4"/>
    
        <!-- |== -->
        <set mask="110-111-110" tiles="11,7"/>
        <!-- _||_ -->
        <set mask="010-111-111" tiles="7,4"/>
        <!-- ==| -->
        <set mask="011-111-011" tiles="4,7"/>
        <!-- T||T -->
        <set mask="111-111-010" tiles="7,11"/>
    
        <!-- ???? -->
        <set mask="010-111-110" tiles="3,2"/>
        <!-- ???? -->
        <set mask="010-111-011" tiles="4,2"/>
        <!-- ???? -->
        <set mask="011-111-010" tiles="4,1"/>
        <!-- ???? -->
        <set mask="110-111-010" tiles="3,1"/>
        <!-- ???? -->
        <set mask="010-111-010" tiles="3,3"/>
        <!-- ???? -->
        <set mask="110-111-011" tiles="3,4"/>
        <!-- ???? -->
        <set mask="011-111-110" tiles="4,3"/>
    
        <set mask="padding" tiles="6,6; 7,6; 8,6; 9,6;  6,7; 6,8; 6,9;  9,7; 9,8; 9,9;  7,9; 8,9"/>
        <set mask="center" tiles="7,7; 8,7; 7,8; 8,8"/>
      </Tileset>
    """;
}