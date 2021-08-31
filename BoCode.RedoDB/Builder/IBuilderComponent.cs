namespace BoCode.RedoDB.Builder
{
    /// <summary>
    /// Components of the RedoDBEngine are prepared inside the builder. 
    /// With this interfacer we enable the builder to interact with the 
    /// component.
    /// </summary>
    public interface IBuilderComponent
    {
        /// <summary>
        /// The component implementing this interface must raise 
        /// MissingBuilderConfiguratmion if the component is not completely
        /// Configured and for this reason the RedoDBEngine can't be built.
        /// </summary>
        public void AssertBuildReady();
        /// <summary>
        /// If on the builder WithNoPersitence is selected, each component of the engine
        /// get informed about this configuration. Components may react to this configuration
        /// in different ways when calling method like "TakeSnapshot" clearly requiring persistence
        /// </summary>
        public void NoPersistence();
    }

    public interface INoPersitence
    {

    }
}
