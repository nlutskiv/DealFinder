export interface BlogPost {
  slug: string;
  title: string;
  date: string;
  excerpt: string;
  category: string;
  readTime: number;
  body: string;
}

export const BLOG_POSTS: BlogPost[] = [
  {
    slug: 'how-to-buy-a-used-gpu',
    title: 'How to Buy a Used GPU Without Getting Burned',
    date: '2026-06-15',
    excerpt: 'New GPU prices are a mess right now. If you\'re going second-hand, here\'s how to actually check what you\'re buying before handing over cash.',
    category: 'Buying Guide',
    readTime: 6,
    body: `
<p>New GPU prices are in a rough place. For a lot of people the only realistic option is buying used, but used comes with risks, and most guides skip the stuff that actually matters. Here's a practical walkthrough, from the listing all the way through testing.</p>

<h2>Reading the listing</h2>

<p>Start with the description. Mining cards aren't automatically bad. If one was used in a well-ventilated rig at sensible clocks, it could be fine. Problem is you have no way to verify the conditions. If a card does come with warranty, don't assume it transfers to you. Some brands require the original proof of purchase and won't honour claims from a second owner. So realistically: aim for cards from someone's gaming PC.</p>

<p>Once you've read the description, look at the photos carefully. The scam has moved on from obvious BIOS flashes on budget cards. High-end fakes now include stripped cards, relabelled chips, swapped coolers, and cards that look completely legitimate from the outside. GPU-Z can catch a lot of these, but don't treat it as the only check. More on that in the testing section.</p>

<p>Also check the PCB for discolouration, burn marks, leaking capacitors, and general dust buildup. Scratches and cracked components are obvious red flags. A warranty seal on the screws suggests the card hasn't been opened, but it's not a guarantee; seals can be removed cleanly with the right tools.</p>

<h2>Checking the seller</h2>

<p>If the listing looks fine, go through their profile and look at past sales. If they've recently sold multiple GPUs, or GPUs alongside high-wattage PSUs, they're probably a miner. Not automatically a dealbreaker, but factor it in.</p>

<p>Be careful with resellers. Some are fine, but others flip borderline or repaired cards with little history and no comeback if it dies on you. You need stronger buyer protection than you would with someone selling their own card.</p>

<h2>Calling before you go</h2>

<p>Message if you have to, but call if you can. It's harder to lie on the spot than it is in a chat. Ask: when was it bought, do they have the box or receipt, how long did it actually run, what temperatures did it hit, and whether the fans make any noise. If it's a mining card, ask them to flash it back to stock BIOS before you arrive.</p>

<p>On payment: avoid bank transfer for anything expensive unless you fully trust the seller. Prefer platforms with buyer protection, PayPal Goods and Services, or collection in person where you can test before handing over money.</p>

<h2>Inspecting in person</h2>

<p>Photos miss things. Pick the card up and look properly. A burnt phase can be hidden under the backplate, with the component quietly cut off. Cards like that can sometimes limp along at a reduced power limit, but you really don't want to be the one who buys one.</p>

<p>If nothing looks damaged or chipped, put it in a PC and start testing.</p>

<h2>Testing</h2>

<p>First thing: confirm the card is actually what it says it is. Google the card's expected specs (shader count, bus width, VRAM bandwidth), then run <strong>GPU-Z</strong> and compare. It flags obvious fakes in the card name with <code>[FAKE]</code>, but also manually check the GPU chip name, die size, and bus width against what the card should have. GPU-Z can catch a lot of fake or misreported cards, but don't treat it as the only check. Sophisticated scams involve cards that don't enumerate properly at all. Verify the boost clock too.</p>

<p>Download <strong>HWiNFO64</strong> and leave it running in the background. It can show extra sensor data like hotspot, VRAM, and sometimes VRM temperatures depending on the card. The basic tools won't give you this.</p>

<p>Now run <strong>FurMark</strong>. Hit GPU stress test and let it run for 15–20 minutes. Watch two things:</p>

<ul>
  <li><strong>Temperature:</strong> different cards have different normal ranges. An RTX 3080 running in the low 80s under FurMark might be perfectly normal for some models, while the same core temperature on a lower-power card could be a warning sign. Always compare against reviews for the exact model, as case airflow, ambient temp, and whether you're reading core vs hotspot all affect the number. Factor in that the thermal paste on a used card is probably dried out and the heatsink is dusty, so some extra heat is expected.</li>
  <li><strong>Boost clock:</strong> compare the stable clock it holds in FurMark against the rated boost from GPU-Z. Some drop is normal, but if it's significantly lower the card is throttling, which points to thermals, a damaged power delivery, or both.</li>
</ul>

<p>While FurMark is running, spin fans up to 100% in <strong>MSI Afterburner</strong> and listen. Any rattling or grinding at full speed will only get worse at lower RPMs over time.</p>

<p>Next, run <strong>OCCT</strong>. Start with the 3D test at maximum shader complexity for around 15 minutes, this is a heavier load than FurMark. It should complete with zero errors. Clocks will be lower than the FurMark run and temps might be a touch higher, that's expected. Then switch to the VRAM tab and run the memory test at maximum load for another 15 minutes. Zero errors here too.</p>

<p>One caveat: FurMark is a DX11-era tool and doesn't fully stress modern architectures like RTX 40-series or RX 7000-series the same way a real game does. If you're buying anything from the last two generations, also run <strong>Unigine Superposition</strong> or <strong>3DMark Time Spy</strong> and compare the score against published results for that exact GPU. A card can display correctly and survive a short stress test while still performing well below where it should. A score check catches that. Crashes or driver timeouts during any of this are an immediate walk away.</p>

<p>If the seller won't give you time to run tests, only buy through a platform with a return window or buyer protection. Don't hand over cash blind.</p>

<h2>Tools you need</h2>

<ul>
  <li><a href="https://www.techpowerup.com/gpuz/" target="_blank" rel="noopener noreferrer">GPU-Z</a></li>
  <li><a href="https://www.hwinfo.com/" target="_blank" rel="noopener noreferrer">HWiNFO64</a></li>
  <li><a href="https://www.msi.com/Landing/afterburner" target="_blank" rel="noopener noreferrer">MSI Afterburner</a></li>
  <li><a href="https://geeks3d.com/furmark/" target="_blank" rel="noopener noreferrer">FurMark</a></li>
  <li><a href="https://www.ocbase.com/" target="_blank" rel="noopener noreferrer">OCCT</a></li>
</ul>

<p>Download these beforehand if you're testing at the seller's place. Don't count on their internet being fast.</p>
    `
  }
];
