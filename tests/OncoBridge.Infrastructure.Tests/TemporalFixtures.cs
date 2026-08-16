namespace OncoBridge.Infrastructure.Tests;

internal static class TemporalFixtures
{
    internal const string InstantOnsetBundle =
        """
        {"resourceType":"Bundle","type":"collection","entry":[
          {"fullUrl":"urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa",
           "resource":{"resourceType":"Patient","id":"patient-001","birthDate":"1968"}},
          {"fullUrl":"urn:uuid:bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb",
           "resource":{"resourceType":"Condition","id":"condition-001",
             "meta":{"profile":[
               "http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-primary-cancer-condition"]},
             "code":{"coding":[{"system":"http://snomed.info/sct","code":"254837009"}]},
             "subject":{"reference":"urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa"},
             "onsetDateTime":"2019-03-14T10:00:00+02:00"}}]}
        """;

    internal const string OpenEndedPeriodBundle =
        """
        {"resourceType":"Bundle","type":"collection","entry":[
          {"fullUrl":"urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa",
           "resource":{"resourceType":"Patient","id":"patient-001","birthDate":"1968"}},
          {"fullUrl":"urn:uuid:bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb",
           "resource":{"resourceType":"Condition","id":"condition-001",
             "meta":{"profile":[
               "http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-primary-cancer-condition"]},
             "code":{"coding":[{"system":"http://snomed.info/sct","code":"254837009"}]},
             "subject":{"reference":"urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa"}}},
          {"fullUrl":"urn:uuid:99999999-7777-4777-8777-999999999999",
           "resource":{"resourceType":"Procedure","id":"procedure-001",
             "meta":{"profile":[
               "http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-cancer-related-surgical-procedure"]},
             "status":"completed",
             "code":{"coding":[{"system":"http://snomed.info/sct","code":"392021009"}]},
             "subject":{"reference":"urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa"},
             "performedPeriod":{"start":"2019"}}}]}
        """;

    internal const string NothingNormalizableBundle =
        """
        {"resourceType":"Bundle","type":"collection","entry":[
          {"fullUrl":"urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa",
           "resource":{"resourceType":"Patient","id":"patient-001","birthDate":"1968"}},
          {"fullUrl":"urn:uuid:bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb",
           "resource":{"resourceType":"Condition","id":"condition-001",
             "code":{"coding":[{"system":"http://snomed.info/sct","code":"254837009"}]},
             "subject":{"reference":"urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa"}}}]}
        """;
}
