# frozen_string_literal: true
require 'iso8601'

module MicrosoftKiotaAbstractions
  # Wrapper Class for ISO8601::Duration
  # Integer support for :years, :months, :weeks, :days, :hours, :minutes, :seconds
  # Initialize with a hash of symbols to integers eg { :years => 3, :days => 4, seconds: => 2}
  # or with an ISO8601 formated string eg "PT3H12M5S".
  class ISODuration
    attr_reader :years, :months, :weeks, :days, :hours, :minutes, :seconds

    UNITS = { :years => 'Y', 
              :months => 'M', 
              :weeks => 'W', 
              :days => 'D', 
              :hours => 'H', 
              :minutes => 'M',
              :seconds => 'S'}

    CONVERSIONS = {
      :ms_to_s => 1000,
      :s_to_m => 60,
      :m_to_h => 60,
      :h_to_d => 24,
      :d_to_w => 7,
      :m_to_y => 12
    }
    def initialize(input)
      if input.is_a? String
        @duration_obj = ISO8601::Duration.new(input)
      elsif input.is_a? Hash
        @duration_obj = parse_hash(input)
      else
        raise StandardError, 'Must provide initialize ISODuration by providing a hash or an ISO8601-formatted string.'
      end
      update_member_variables
      normalize
    end

    def string
      input = { :seconds => @seconds, :minutes => @minutes, :hours => @hours, 
                :days => @days, :weeks => @weeks, :months => @months, 
                :years => @years } 
      iso_str = 'P'
      UNITS.each do |unit, abrev|
        iso_str += input[unit].to_s + abrev unless input[unit].zero?
        iso_str += 'T' if unit == :days
      end
      iso_str = iso_str.strip
      iso_str = iso_str.chomp('T') if (iso_str[-1]).eql? 'T'
      iso_str
    end

    def normalize
      if @seconds >= CONVERSIONS[:s_to_m]
        @minutes += (@seconds / CONVERSIONS[:s_to_m]).floor
        @seconds %= CONVERSIONS[:s_to_m]
      end
      if @minutes >= CONVERSIONS[:m_to_h]
        @hours += (@minutes / CONVERSIONS[:m_to_h]).floor
        @minutes %= CONVERSIONS[:m_to_h]
      end
      if @hours >= CONVERSIONS[:h_to_d]
        @days += (@hours / CONVERSIONS[:h_to_d]).floor
        @hours %= CONVERSIONS[:h_to_d]
      end
      if @days >= CONVERSIONS[:d_to_w] && @months == 0 && @years == 0
        @weeks += (@days / CONVERSIONS[:d_to_w]).floor
        @days %= CONVERSIONS[:d_to_w]
      end
      if @months > CONVERSIONS[:m_to_y]
        @years += (@months / CONVERSIONS[:m_to_y]).floor
        @months %= CONVERSIONS[:m_to_y]
      end
    end

    def seconds=(value)
      input = { :seconds => value, :minutes => @minutes, :hours => @hours, 
                :days => @days, :weeks => @weeks, :months => @months, 
                :years => @years } 
      @duration_obj = parse_hash(input)
      @seconds = value
      normalize
    end

    def minutes=(value)
      input = { :seconds => @seconds, :minutes => value, :hours => @hours, 
                :days => @days, :weeks => @weeks, :months => @months, 
                :years => @years } 
      @duration_obj = parse_hash(input)
      @minutes = value
      normalize
    end

    def hours=(value)
      input = { :seconds => @seconds, :minutes => @minutes, :hours => value, 
                :days => @days, :weeks => @weeks, :months => @months, 
                :years => @years } 
      @duration_obj = parse_hash(input)
      @hours = value
      normalize
    end

    def days=(value)
      input = { :seconds => @seconds, :minutes => @minutes, :hours => @hours, 
                :days => value, :weeks => @weeks, :months => @months, 
                :years => @years } 
      @duration_obj = parse_hash(input)
      @days = value
      normalize
    end

    def weeks=(value)
      input = { :seconds => @seconds, :minutes => @minutes, :hours => @hours, 
                :days => @days, :weeks => value, :months => @months, 
                :years => @years } 
      @duration_obj = parse_hash(input)
      @weeks = value
      normalize
    end

    def months=(value)
      input = { :seconds => @seconds, :minutes => @minutes, :hours => @hours, 
                :days => @days, :weeks => @weeks, :months => value, 
                :years => @years } 
      @duration_obj = parse_hash(input)
      @months = value
      normalize
    end

    def years=(value)
      input = { :seconds => @seconds, :minutes => @minutes, :hours => @hours, 
        :days => @days, :weeks => @weeks, :months => @months, 
        :years => value } 
      @duration_obj = parse_hash(input)
      @years = value
      normalize
    end

    def abs
      @duration_obj = @duration_obj.abs
      update_member_variables
      return self
    end

    def +(other)
      new_obj = self.duration_obj + other.duration_obj
      MicrosoftKiotaAbstractions::ISODuration.new(dur_obj_to_hash(new_obj))
    end

    def -(other)
      new_obj = self.duration_obj - other.duration_obj
      MicrosoftKiotaAbstractions::ISODuration.new(dur_obj_to_hash(new_obj))
    end

    def ==(other)
      @duration_obj == other.duration_obj
    end

    def -@
      @duration_obj = -@duration_obj
      update_member_variables
    end

    def eql?(other)
      @duration_obj == other.duration_obj
    end

    protected

    attr_accessor :duration_obj

    def parse_hash(input)
      iso_str = 'P'
      input.each do |keys, values|
        raise StandardError, "The key #{keys} is not recognized" unless UNITS.key?(keys)
      end
      UNITS.each do |unit, abrev|
        iso_str += input[unit].to_s + abrev if input.key?(unit) && !input[unit].zero?
        iso_str += 'T' if unit == :days
      end
      iso_str = iso_str.strip
      iso_str = iso_str.chomp('T') if (iso_str[-1]).eql? 'T'
      ISO8601::Duration.new(iso_str)
    end

    def update_member_variables
      @seconds = @duration_obj.seconds.nil? ? 0 : ((@duration_obj.seconds.to_s).split('S')[0]).to_i
      @minutes = @duration_obj.minutes.nil? ? 0 : ((@duration_obj.minutes.to_s).split('H')[0]).to_i
      @hours = @duration_obj.hours.nil? ? 0 : ((@duration_obj.hours.to_s).split('H')[0]).to_i
      @days = @duration_obj.days.nil? ? 0 : ((@duration_obj.days.to_s).split('D')[0]).to_i
      @weeks = @duration_obj.weeks.nil? ? 0 : ((@duration_obj.weeks.to_s).split('W')[0]).to_i
      @months = @duration_obj.months.nil? ? 0 : ((@duration_obj.months.to_s).split('M')[0]).to_i
      @years = @duration_obj.years.nil? ? 0 : ((@duration_obj.years.to_s).split('Y')[0]).to_i
    end

    def dur_obj_to_hash(dur_obj)
      result_hash = {}
      result_hash[:seconds] = dur_obj.seconds.nil? ? 0 : ((dur_obj.seconds.to_s).split('S')[0]).to_i
      result_hash[:minutes] = dur_obj.minutes.nil? ? 0 : ((dur_obj.minutes.to_s).split('H')[0]).to_i
      result_hash[:hours] = dur_obj.hours.nil? ? 0 : ((dur_obj.hours.to_s).split('H')[0]).to_i
      result_hash[:days] = dur_obj.days.nil? ? 0 : ((dur_obj.days.to_s).split('D')[0]).to_i
      result_hash[:weeks] = dur_obj.weeks.nil? ? 0 : ((dur_obj.weeks.to_s).split('W')[0]).to_i
      result_hash[:months] = dur_obj.months.nil? ? 0 : ((dur_obj.months.to_s).split('M')[0]).to_i
      result_hash[:years] = dur_obj.years.nil? ? 0 : ((dur_obj.years.to_s).split('Y')[0]).to_i
      result_hash
    end
  end
end
