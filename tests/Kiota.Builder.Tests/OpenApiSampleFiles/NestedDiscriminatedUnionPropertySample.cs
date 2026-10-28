namespace Kiota.Builder.Tests.OpenApiSampleFiles;

public static class NestedDiscriminatedUnionPropertySample
{
    /**
    * An OpenAPI 3.1.0 sample document where discriminated unions are nested: Invoice.lines is a
    * collection of the InvoiceLine union, and each line member has a vat property typed as the
    * LineVat union. LineVat maps several discriminator values to one member (export and exempt
    * both map to TaxFreeVat). Receipt.payment is an inline oneOf with a discriminator, and
    * Receipt.note is a oneOf without one.
    */
    public static readonly string OpenApiYaml = @"
openapi: 3.1.0
info:
  title: Invoice API
  version: 1.0.0
paths:
  /invoice:
    get:
      responses:
        '200':
          description: An invoice
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Invoice'
  /receipt:
    get:
      responses:
        '200':
          description: A receipt
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Receipt'
components:
  schemas:
    Invoice:
      type: object
      properties:
        number:
          type: string
        lines:
          type: array
          items:
            $ref: '#/components/schemas/InvoiceLine'
    InvoiceLine:
      oneOf:
        - $ref: '#/components/schemas/NetLine'
        - $ref: '#/components/schemas/GrossLine'
      discriminator:
        propertyName: statedAs
        mapping:
          net: '#/components/schemas/NetLine'
          gross: '#/components/schemas/GrossLine'
    NetLine:
      type: object
      properties:
        statedAs:
          type: string
          enum: [net]
        unitNetPrice:
          type: string
        vat:
          $ref: '#/components/schemas/LineVat'
      required: [statedAs]
    GrossLine:
      type: object
      properties:
        statedAs:
          type: string
          enum: [gross]
        unitGrossPrice:
          type: string
        vat:
          $ref: '#/components/schemas/LineVat'
      required: [statedAs]
    LineVat:
      oneOf:
        - $ref: '#/components/schemas/StandardVat'
        - $ref: '#/components/schemas/TaxFreeVat'
      discriminator:
        propertyName: category
        mapping:
          standard: '#/components/schemas/StandardVat'
          export: '#/components/schemas/TaxFreeVat'
          exempt: '#/components/schemas/TaxFreeVat'
    StandardVat:
      type: object
      properties:
        category:
          type: string
          enum: [standard]
        rate:
          type: string
      required: [category, rate]
    TaxFreeVat:
      type: object
      properties:
        category:
          type: string
          enum: [export, exempt]
      required: [category]
    Receipt:
      type: object
      properties:
        payment:
          oneOf:
            - $ref: '#/components/schemas/CardPayment'
            - $ref: '#/components/schemas/CashPayment'
          discriminator:
            propertyName: method
            mapping:
              card: '#/components/schemas/CardPayment'
              cash: '#/components/schemas/CashPayment'
        note:
          oneOf:
            - $ref: '#/components/schemas/TextNote'
            - $ref: '#/components/schemas/LinkNote'
    CardPayment:
      type: object
      properties:
        method:
          type: string
          enum: [card]
        last4:
          type: string
      required: [method]
    CashPayment:
      type: object
      properties:
        method:
          type: string
          enum: [cash]
        tendered:
          type: string
      required: [method]
    TextNote:
      type: object
      properties:
        text:
          type: string
    LinkNote:
      type: object
      properties:
        url:
          type: string";
}
