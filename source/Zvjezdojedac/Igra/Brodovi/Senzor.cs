﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Prototip
{
	public class Senzor : Komponenta<Senzor.SenzorInfo>
	{
		public static double BonusKolicine(double kolicina)
		{
			return Math.Pow(kolicina, 1 / 3.0);
		}

		public class SenzorInfo : AKomponentaInfo
		{
			#region Statično
			public static List<SenzorInfo> Senzori = new List<SenzorInfo>();

			public static void UcitajSenzorInfo(Dictionary<string, string> podaci)
			{
				string naziv = podaci["IME"];
				string opis = podaci["OPIS"];
				Image slika = Image.FromFile(podaci["SLIKA"]);
				List<Tehnologija.Preduvjet> preduvjeti = Tehnologija.Preduvjet.NaciniPreduvjete(podaci["PREDUVJETI"]);
				int maxNivo = int.Parse(podaci["MAX_NIVO"]);

				Formula razlucivost = Formula.IzStringa(podaci["RAZLUCIVOST"]);

				Senzori.Add(new SenzorInfo(
					naziv, opis, slika, preduvjeti, maxNivo,
					razlucivost)
					);
			}

			public static Senzor NajboljiSenzor(Dictionary<string, double> varijable)
			{
				double max = double.MinValue;
				Senzor naj = null;

				foreach (SenzorInfo si in Senzori)
					if (si.dostupno(varijable, 0))
					{
						Senzor trenutni = si.naciniKomponentu(varijable);

						if (trenutni.razlucivost > max)
						{
							max = trenutni.razlucivost;
							naj = trenutni;
						}
					}

				return naj;
			}
			#endregion

			private Formula razlucivost;

			private SenzorInfo(string naziv, string opis, Image slika,
				List<Tehnologija.Preduvjet> preduvjeti, int maxNivo,
				Formula razlucivost)
				:
				base(naziv, opis, slika, preduvjeti, maxNivo)
			{
				this.razlucivost = razlucivost;
			}

			public Senzor naciniKomponentu(Dictionary<string, double> varijable)
			{
				int nivo = maxDostupanNivo(varijable);
				return new Senzor(
					this, 
					nivo,
					Evaluiraj(razlucivost, nivo));
			}
		}

		/// <summary>
		/// Sposobnost senzora da detektira prikrivene brodove.
		/// </summary>
		public double razlucivost { get; private set; }

		public Senzor(SenzorInfo info, int nivo, double razlucivost)
			: base(info, nivo)
		{
			this.razlucivost = razlucivost;
		}
	}
}
