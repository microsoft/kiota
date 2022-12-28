require 'microsoft_kiota_abstractions'
require_relative 'request_option_mock'
require_relative 'query_parameters_mock'

RSpec.describe MicrosoftKiotaAbstractions do

    it "adds a request option to the request information" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        mock_option = RequestOptionMock.new
        mock_option.value = "value"
        request_info.add_request_options(mock_option)
        expect(request_info.get_request_options().length).to eq(1)
        expect(request_info.get_request_option("key")).to eq(mock_option)
    end

    it "removes a request option from the request information" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        mock_option = RequestOptionMock.new
        mock_option.value = "value"
        request_info.add_request_options(mock_option)
        expect(request_info.get_request_options().length).to eq(1)
        request_info.remove_request_options("key")
        expect(request_info.get_request_options().length).to eq(0)
    end

    it "adds query parameters with their escaped values" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        query_parameters = QueryParametersMock.new
        query_parameters.select = "foo,bar"
        request_info.set_query_string_parameters_from_raw_object(query_parameters)
        expect(request_info.query_parameters.length).to eq(1)
        expect(request_info.query_parameters["%24select"]).to eq("foo,bar")
        expect(request_info.query_parameters["select"]).to be_nil
    end

    it "doesn't fail adding query parameters with anonymous object" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        query_parameters = {
            select: "foo,bar"
        }
        request_info.set_query_string_parameters_from_raw_object(query_parameters)
        expect(request_info.query_parameters.length).to eq(0)
    end

end