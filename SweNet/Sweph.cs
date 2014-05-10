﻿using SwissEphNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SweNet
{

    /// <summary>
    /// Swiss Ephemeris engine
    /// </summary>
    public class Sweph : IDisposable
    {
        bool _Initialized = false;
        SwissEphNet.SwissEph _SwissEph;
        SweDate _Date;
        Persit.IDataProvider _DataProvider;
        SwePlanet _Planets;

        #region Public constants

        /// <summary>
        /// Current Swiss Ephemeris version
        /// </summary>
        public const String Version = "2.00.00";

        #endregion

        #region Ctors & Dest

        /// <summary>
        /// Create a new context
        /// </summary>
        public Sweph(SweConfig config = null) {
            _Initialized = false;
            if (config == null)
                this.Config = new SweConfig();
            else
                this.Config = config.Clone();
        }

        /// <summary>
        /// Internal release resources
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                Close();
                _Initialized = true;
            }
        }

        /// <summary>
        /// Release resources
        /// </summary>
        public void Dispose() {
            Dispose(true);
        }

        #endregion

        #region General methods

        /// <summary>
        /// Check if engine is initialized
        /// </summary>
        protected void CheckInitialized() {
            if (!_Initialized) {
                _Initialized = true;
                Initialize();
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        protected virtual void Initialize(){
            _Date = CreateDateEngine();
            _DataProvider = CreateDataProvider();
            _Planets = CreatePlanetsEngine();
            _SwissEph = new SwissEph();
            _SwissEph.OnLoadFile += (s, e) => {
                e.File = LoadFile(e.FileName);
            };
        }

        /// <summary>
        /// Internal close
        /// </summary>
        void Close() {
            if (_SwissEph != null)
                _SwissEph.Dispose();
            _SwissEph = null;
            _Date = null;
            _DataProvider = null;
        }

        /// <summary>
        /// Check default encoding for Swiss Ephemeris
        /// </summary>
        public static Encoding CheckEncoding(Encoding encoding) {
            return encoding ?? Encoding.GetEncoding("Windows-1252");
        }

        #endregion

        #region Swiss Ephemeris proxies

        /// <summary>
        /// swe_set_topo()
        /// </summary>
        public void swe_set_topo(double geolon, double geolat, double height) {
            SwissEph.swe_set_topo(geolon, geolat, height);
        }

        /// <summary>
        /// swe_calc()
        /// </summary>
        public Int32 swe_calc(double tjd, int ipl, Int32 iflag, double[] xx, ref string serr) {
            return SwissEph.swe_calc(tjd, ipl, iflag, xx, ref serr);
        }

        /// <summary>
        /// swe_sidtime()
        /// </summary>
        public double swe_sidtime(double tjd_ut) { return SwissEph.swe_sidtime(tjd_ut); }

        /// <summary>
        /// swe_fixstar()
        /// </summary>
        public Int32 swe_fixstar(string star, double tjd, Int32 iflag, double[] xx, ref string serr) {
            return SwissEph.swe_fixstar(star, tjd, iflag, xx, ref serr);
        }

        /// <summary>
        /// swe_get_planet_name()
        /// </summary>
        public string swe_get_planet_name(int ipl) { return SwissEph.swe_get_planet_name(ipl); }

        /// <summary>
        /// swe_houses()
        /// </summary>
        public int swe_houses(double tjd_ut, double geolat, double geolon, char hsys, double[] cusps, double[] ascmc) {
            return SwissEph.swe_houses(tjd_ut, geolat, geolon, hsys, cusps, ascmc);
        }

        /// <summary>
        /// swe_houses_ex()
        /// </summary>
        public int swe_houses_ex(double tjd_ut, Int32 iflag, double geolat, double geolon, char hsys, CPointer<double> hcusps, CPointer<double> ascmc) {
            return SwissEph.swe_houses_ex(tjd_ut, iflag, geolat, geolon, hsys, hcusps, ascmc);
        }

        /// <summary>
        /// swe_house_pos()
        /// </summary>
        public double swe_house_pos(double armc, double geolon, double eps, char hsys, double[] xpin, ref string serr) {
            return SwissEph.swe_house_pos(armc, geolon, eps, hsys, xpin, ref serr);
        }

        #endregion

        #region Data management

        /// <summary>
        /// Create a new data provider
        /// </summary>
        /// <returns></returns>
        protected virtual Persit.IDataProvider CreateDataProvider() {
            return new Persit.EmptyDataProvider();
        }

        #endregion

        #region Date management

        /// <summary>
        /// Create a new date engine
        /// </summary>
        protected virtual SweDate CreateDateEngine() {
            return new SweDate(this, Config.UseEspenakMeeusDeltaT);
        }

        /// <summary>
        /// Create a Julian Day
        /// </summary>
        public JulianDay JulianDay(int year, int month, int day, double hour, DateCalendar? calendar = null) {
            return JulianDay(new DateUT(year, month, day, hour), calendar);
        }

        /// <summary>
        /// Create a Julian Day
        /// </summary>
        public JulianDay JulianDay(int year, int month, int day, int hour, int minute, int second, DateCalendar? calendar = null) {
            return JulianDay(new DateUT(year, month, day, hour, minute, second), calendar);
        }

        /// <summary>
        /// Create a Julian Day
        /// </summary>
        public JulianDay JulianDay(DateUT date, DateCalendar? calendar = null) {
            return new JulianDay(date, calendar);
        }

        /// <summary>
        /// Create an Ephemeris Time
        /// </summary>
        public EphemerisTime EphemerisTime(JulianDay day) {
            return new EphemerisTime(day, Date.DeltaT(day));
        }

        /// <summary>
        /// Get Date UT from Julian Day
        /// </summary>
        public DateUT DateUT(JulianDay jd) {
            return SweDate.JulianDayToDate(jd.Value, jd.Calendar);
        }

        /// <summary>
        /// Get Date UT from Ephemeris Time
        /// </summary>
        public DateUT DateUT(EphemerisTime et) {
            return SweDate.JulianDayToDate(et.JulianDay.Value, et.JulianDay.Calendar);
        }

        #endregion

        #region Planets management

        /// <summary>
        /// Create a new planets envgine
        /// </summary>
        protected virtual SwePlanet CreatePlanetsEngine(){
            return new SwePlanet(this);
        }

        /// <summary>
        /// Get a planet name
        /// </summary>
        /// <param name="planetId">Id of planet</param>
        /// <returns>Name</returns>
        public String PlanetName(Planet planetId) {
            return Planets.GetPlanetName(planetId);
        }

        #endregion

        #region File management

        /// <summary>
        /// Load a file
        /// </summary>
        /// <param name="filename">File name</param>
        /// <returns>File loaded or null if file not found</returns>
        internal protected Stream LoadFile(String filename) {
            var h = OnLoadFile;
            if (h != null) {
                var e = new LoadFileEventArgs(filename);
                h(this, e);
                return e.File;
            }
            return null;
        }

        #endregion

        #region Trace

        /// <summary>
        /// Trace information
        /// </summary>
        public void Trace(String format, params object[] args) {
            var h = OnTrace;
            if (h != null) {
                String message = args != null ? String.Format(format, args) : format;
                h(this, new TraceEventArgs(message));
            }
        }

        #endregion

        #region Protected properties

        /// <summary>
        /// Current configuration
        /// </summary>
        protected SweConfig Config { get; private set; }

        /// <summary>
        /// Swiss Ephemeris
        /// </summary>
        protected SwissEphNet.SwissEph SwissEph { get { CheckInitialized(); return _SwissEph; } }

        #endregion

        #region Current engines

        /// <summary>
        /// Current date engine
        /// </summary>
        public SweDate Date { get { CheckInitialized(); return _Date; } }

        /// <summary>
        /// Current data provider
        /// </summary>
        public Persit.IDataProvider DataProvider { get { CheckInitialized(); return _DataProvider; } }

        /// <summary>
        /// Current planets engine
        /// </summary>
        public SwePlanet Planets { get { CheckInitialized(); return _Planets; } }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when a new trace message is invoked
        /// </summary>
        public event EventHandler<TraceEventArgs> OnTrace;

        /// <summary>
        /// Event raised when loading a file is required
        /// </summary>
        public event EventHandler<LoadFileEventArgs> OnLoadFile;

        #endregion

    }

}
