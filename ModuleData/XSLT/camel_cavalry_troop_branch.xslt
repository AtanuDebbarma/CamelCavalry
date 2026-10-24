<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output omit-xml-declaration="yes"/>

	<!-- Adds one camel upgrade next to the vanilla upgrades; nothing vanilla is removed. Match patterns repeat the IDs because XSLT 1.0 forbids variables there. -->
	<xsl:variable name="aseraiTarget">NPCCharacter.camel_cavalry_aserai_camel_rider</xsl:variable>
	<xsl:variable name="khuzaitTarget">NPCCharacter.camel_cavalry_khuzait_camel_torguud</xsl:variable>

	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="NPCCharacter[@id='aserai_mameluke_regular']/upgrade_targets[not(upgrade_target[@id='NPCCharacter.camel_cavalry_aserai_camel_rider'])]">
		<xsl:call-template name="append-target">
			<xsl:with-param name="target" select="$aseraiTarget"/>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="NPCCharacter[@id='aserai_mameluke_regular'][not(upgrade_targets)]">
		<xsl:call-template name="add-targets">
			<xsl:with-param name="target" select="$aseraiTarget"/>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="NPCCharacter[@id='khuzait_qanqli']/upgrade_targets[not(upgrade_target[@id='NPCCharacter.camel_cavalry_khuzait_camel_torguud'])]">
		<xsl:call-template name="append-target">
			<xsl:with-param name="target" select="$khuzaitTarget"/>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="NPCCharacter[@id='khuzait_qanqli'][not(upgrade_targets)]">
		<xsl:call-template name="add-targets">
			<xsl:with-param name="target" select="$khuzaitTarget"/>
		</xsl:call-template>
	</xsl:template>

	<xsl:template name="append-target">
		<xsl:param name="target"/>
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
			<upgrade_target id="{$target}"/>
		</xsl:copy>
	</xsl:template>

	<xsl:template name="add-targets">
		<xsl:param name="target"/>
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
			<upgrade_targets>
				<upgrade_target id="{$target}"/>
			</upgrade_targets>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
