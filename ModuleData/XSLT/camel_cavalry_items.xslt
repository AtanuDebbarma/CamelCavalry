<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" />

  <!-- Preserve every item loaded before Camel Cavalry unless a template below changes it. -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>

  <!-- Camel Armor BM168 owns these items and meshes. Camel Cavalry only makes the four
       harnesses ordinary merchandise in the current game's horse-equipment market. -->
  <xsl:template match="Item[@id='camel_scale_armor_1']">
    <xsl:copy>
      <xsl:apply-templates select="@*[name() != 'item_category' and name() != 'value' and name() != 'is_merchandise']" />
      <xsl:attribute name="item_category">horse_equipment_3</xsl:attribute>
      <xsl:attribute name="value">4000</xsl:attribute>
      <xsl:attribute name="is_merchandise">true</xsl:attribute>
      <xsl:apply-templates select="node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="Item[@id='camel_lamellar_armor_1' or @id='camel_lamellar_armor_1b']">
    <xsl:copy>
      <xsl:apply-templates select="@*[name() != 'item_category' and name() != 'value' and name() != 'is_merchandise']" />
      <xsl:attribute name="item_category">horse_equipment_3</xsl:attribute>
      <xsl:attribute name="value">4800</xsl:attribute>
      <xsl:attribute name="is_merchandise">true</xsl:attribute>
      <xsl:apply-templates select="node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="Item[@id='camel_chainmail_armor_1']">
    <xsl:copy>
      <xsl:apply-templates select="@*[name() != 'item_category' and name() != 'value' and name() != 'is_merchandise']" />
      <xsl:attribute name="item_category">horse_equipment_3</xsl:attribute>
      <xsl:attribute name="value">3200</xsl:attribute>
      <xsl:attribute name="is_merchandise">true</xsl:attribute>
      <xsl:apply-templates select="node()" />
    </xsl:copy>
  </xsl:template>

  <!-- The lance also belongs to Camel Armor BM168 and uses vanilla crafting pieces. -->
  <xsl:template match="CraftedItem[@id='aserai_camel_lance_1']">
    <xsl:copy>
      <xsl:apply-templates select="@*[name() != 'value' and name() != 'is_merchandise']" />
      <xsl:attribute name="value">1200</xsl:attribute>
      <xsl:attribute name="is_merchandise">true</xsl:attribute>
      <xsl:apply-templates select="node()" />
    </xsl:copy>
  </xsl:template>

  <!-- Camel Armor BM168's four armors have a second, empty Flags element after the Civilian flag. -->
  <xsl:template match="Item[
      @id='camel_scale_armor_1' or
      @id='camel_lamellar_armor_1' or
      @id='camel_lamellar_armor_1b' or
      @id='camel_chainmail_armor_1'
    ]/Flags[not(@*) and not(node())]" />
</xsl:stylesheet>
