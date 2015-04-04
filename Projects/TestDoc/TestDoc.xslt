<?xml version="1.0" encoding="iso-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:preserve-space elements="Summary"/>
  <xsl:template match="/">
    <html>
      <head>
        <style type="text/css">
          *   {
          font-family:Segoe UI; font-size:x-small;
          }
          .TestClassSummary {
          font-family:font-size:xx-small; color:gray; font-style:italic
          }
          h2 {font-size:medium;}
        </style>
      </head>
      <body>
        <h2>Automated Test Cases</h2>
        <div>
          <table border="1">
            <tr bgcolor="#9acd32">
              <th>Test Class</th>
              <th>Test Case</th>
              <th>Description</th>
            </tr>
            <xsl:for-each select="TestDoc/TestClass">
              <xsl:variable name="TestCount" select="count(TestMethod)" />
              <xsl:variable name="index" select="1" />
              <xsl:for-each select="TestMethod">
                <tr>
                  <xsl:if test= "position() = 1">
                    <td rowspan="{$TestCount}">
                      <xsl:value-of select="../@Name"/>
                      <br></br>
                      <span class="TestClassSummary">
                        <xsl:call-template name="LFsToBRs">
                          <xsl:with-param name="input" select="../@Summary"/>
                        </xsl:call-template>
                      </span>
                    </td>
                  </xsl:if>
                  <td>
                    <xsl:value-of select="@Name"/>
                  </td>
                  <td>
                    <xsl:call-template name="LFsToBRs">
                      <xsl:with-param name="input" select="Summary"/>
                    </xsl:call-template>
                  </td>
                </tr>
              </xsl:for-each>
            </xsl:for-each>
          </table>
        </div>
      </body>
    </html>
  </xsl:template>
  <xsl:template name="LFsToBRs">
    <xsl:param name="input" />
    <xsl:choose>
      <xsl:when test="contains($input, '&#10;')">
        <xsl:value-of select="substring-before($input, '&#10;')" />
        <br />
        <xsl:call-template name="LFsToBRs">
          <xsl:with-param name="input" select="substring-after($input, '&#10;')" />
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$input" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
