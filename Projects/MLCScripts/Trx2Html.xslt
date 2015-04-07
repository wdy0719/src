<?xml version="1.0" encoding="iso-8859-1"?>
<xsl:stylesheet version="2.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:t="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">

  <xsl:template match="/">
    <b>Summary:</b>
    <table style="font-family:Calibri Light;font-size:smaller;text-align:center;">
      <tr style="font-weight:bold;
          background-color:#EFEFF7;">
        <td width="70">
          Total
        </td>
        <td width="70">
          Executed
        </td>
        <td width="70">
          Failed
        </td>
        <td width="260">
          Time
        </td>
      </tr>
      <tr>
        <td>
          <xsl:value-of select="//t:Counters/@total"/>
        </td>
        <td>
          <xsl:value-of select="//t:Counters/@executed"/>
        </td>
        <td>
		 <xsl:if test="//t:Counters/@failed"><xsl:value-of select="//t:Counters/@failed"/></xsl:if>
			<xsl:if test="not(//t:Counters/@failed)">0</xsl:if>
        </td>
        <td>
          <xsl:value-of select='//t:Times/@finish'/>
        </td>
      </tr>
    </table>
    <hr />
    <br />
    <table style="font-family:Calibri Light;font-size:smaller;">
        <tr style="font-weight:bold;background-color:#EFEFF7;text-align:center;">
        <td>Test Name</td>
        <td width="70">Result</td>
      </tr>
      <xsl:for-each select="//t:UnitTestResult">
        <tr>
          <td>
            <xsl:value-of select="@testName"/>
          </td>
          <td class='{@outcome}'>
			<xsl:variable name="outcome" select='@outcome' />
			<xsl:choose>
				<xsl:when test="$outcome = 'Inconclusive'">
					Known Issue
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="$outcome"/>
				</xsl:otherwise>
			</xsl:choose>
          </td>
        </tr>
      </xsl:for-each>
    </table>
  </xsl:template>
</xsl:stylesheet>
