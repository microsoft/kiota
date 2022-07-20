# frozen_string_literal: true

require 'uri'
require_relative 'spec_helper'

RSpec.describe MicrosoftKiotaAbstractions do
  it 'initializes empty allowed hosts properly' do
    ahv = MicrosoftKiotaAbstractions::AllowedHostsValidator.new([])
    expect(ahv.allowed_hosts).to eq([])
  end

  it 'initializes non-empty/cased allowed hosts properly' do
    ahv = MicrosoftKiotaAbstractions::AllowedHostsValidator.new(['microsoft.com', 'Graph.microsoft.com', 'DOD-graph.microsoft.us'])
    valid_hosts = ahv.allowed_hosts
    expect(valid_hosts).to eq(['microsoft.com', 'graph.microsoft.com', 'dod-graph.microsoft.us'])
  end

  it 'tests the setter for allowed hosts on allowed hosts validator' do
    ahv = MicrosoftKiotaAbstractions::AllowedHostsValidator.new(['microsoft.com', 'Graph.microsoft.com', 'DOD-graph.microsoft.us'])
    ahv.allowed_hosts = ['MICROSOFT.com', 'GRAPH.microsoft.COM', 'DOD-graph.microsoft.us', 'graph.microsoft.de']
    expect(ahv.allowed_hosts).to eq(['microsoft.com', 'graph.microsoft.com', 'dod-graph.microsoft.us', 'graph.microsoft.de'])
  end

  it 'tests url_host_valid? method on malformed and valid urls' do
    ahv = MicrosoftKiotaAbstractions::AllowedHostsValidator.new(['www.google.com', 'example.com', 'Graph.microsoft.com',
                                                                 'DOD-graph.microsoft.us', "cool/groovy/art"])
    url1 = ahv.url_host_valid?("https://www.google.com")
    url2 = ahv.url_host_valid?("htts://google.com")
    url3 = ahv.url_host_valid?("cool/groovy/art")
    url4 = ahv.url_host_valid?("http://example.com")
    url5 = ahv.url_host_valid?('https%3A%2F%2Fwww.example.com')
    url6 = ahv.url_host_valid?('%3A%2F%2F')
    expect(url1).to eq(true)
    expect(url2).to eq(false)
    expect(url3).to eq(false)
    expect(url4).to eq(true)
    expect(url5).to eq(false)
    expect(url5).to eq(false)
    expect(url6).to eq(false)
  end

  it '(client credential context) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAbstractions::ClientCredentialContext.new(nil, nil, nil, {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::ClientCredentialContext.new('tenant_id', 'client_id', 'client_secret', {}) }.not_to raise_error
    expect  { MicrosoftKiotaAbstractions::ClientCredentialContext.new('', 'client_id', 'client_secret', {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::ClientCredentialContext.new('tenant_id', '', 'client_secret', {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::ClientCredentialContext.new('tenant_id', 'client_id', '', {}) }.to raise_error(StandardError)
  end

  it '(auth code context) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAbstractions::AuthorizationCodeContext.new(nil, nil, nil, nil) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::AuthorizationCodeContext.new('tenant_id', 'client_id', 'client_secret', 'redirect_uri') }.not_to raise_error
    expect  { MicrosoftKiotaAbstractions::AuthorizationCodeContext.new('', 'client_id', 'client_secret', 'redirect_uri', 'code') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::AuthorizationCodeContext.new('tenant_id', '', 'client_secret', '') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::AuthorizationCodeContext.new('tenant_id', 'client_id', '', 'redirect_uri') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::AuthorizationCodeContext.new('tenant_id', '', 'client_secret', 'redirect_uri') }.to raise_error(StandardError)
  end


  it '(on behalf of) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAbstractions::OnBehalfOfContext.new(nil, nil, nil, nil) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::OnBehalfOfContext.new('tenant_id', 'client_id', 'client_secret', 'assertion') }.not_to raise_error
    expect  { MicrosoftKiotaAbstractions::OnBehalfOfContext.new('', 'client_id', 'client_secret', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::OnBehalfOfContext.new('tenant_id', '', 'client_secret', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::OnBehalfOfContext.new('tenant_id', 'client_id', '', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAbstractions::OnBehalfOfContext.new('tenant_id', 'client_id', 'client_secret', '') }.to raise_error(StandardError)
  end
end
